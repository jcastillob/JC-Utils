using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net.Mail;
using System.Threading;

namespace JCB_Utils
{
    [System.Serializable]
    public class JCUtils
    {
        public const int MAX_RETRY = 2;
        public const double LONG_WAIT_SECONDS = 5;
        public const double SHORT_WAIT_SECONDS = 0.5;
        public static readonly TimeSpan longWait = TimeSpan.FromSeconds(LONG_WAIT_SECONDS);
        public static readonly TimeSpan shortWait = TimeSpan.FromSeconds(SHORT_WAIT_SECONDS);
        private static string connString;
        public static string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(connString))
                {
                    connString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
                }

                return connString;
            }
        }

        public static DataTable ExecuteSP(string StoredProcedureName, List<Parameter> ParametersList = null,
                                          string SortColumn = "", string connectionString = null)
        {
            return ExecuteSP(StoredProcedureName, ref ParametersList, SortColumn, connectionString);
        }

        public static DataTable ExecuteSP(string StoredProcedureName, ref List<Parameter> ParametersList,
                                          string SortColumn = "", string connectionString = null)
        {
            DataTable toRet = null;
            bool containsOutParameter = false;
            int retryCount = 0;
            string connString = !string.IsNullOrEmpty(connectionString) ? connectionString : JCUtils.ConnectionString;

            do
            {
                SqlCommand cmd = null;
                try
                {

                    using (SqlConnection conn = new SqlConnection(connString))
                    {
                        using (cmd = conn.CreateCommand())
                        {
                            cmd.CommandTimeout = conn.ConnectionTimeout;
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.CommandText = StoredProcedureName;

                            //make sure that we have a parameter
                            if (ParametersList != null && ParametersList.Count > 0)
                            {
                                for (int i = 0; i < ParametersList.Count; i++)
                                    if (ParametersList[i] != null)
                                    {
                                        //Build output parameters along with their sqltype
                                        if (ParametersList[i].IsOutput)
                                        {
                                            cmd.Parameters.Add(ParametersList[i].Name, ParametersList[i].SQLType).Direction = ParameterDirection.Output;

                                            //this will be use to determined if we are expecting an output parameter
                                            containsOutParameter = true;
                                        }
                                        else
                                        {
                                            cmd.Parameters.AddWithValue(ParametersList[i].Name, ParametersList[i].Value);
                                        }
                                    }
                            }

                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                conn.Open();
                                using (DataSet ds = new DataSet())
                                {
                                    da.Fill(ds);

                                    //Sort results if we received a sort column
                                    if (!string.IsNullOrEmpty(SortColumn))
                                        ds.Tables[0].DefaultView.Sort = SortColumn;

                                    //get the first result set to return it, if you get more result sets 
                                    //you need to create an override with the needed functionality
                                    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                        toRet = ds.Tables[0];

                                    //if we have output parameters retrieve their values and assign them into 
                                    //the ParameterList
                                    if (containsOutParameter)
                                    {
                                        for (int i = 0; i < ParametersList.Count; i++)
                                        {
                                            if (ParametersList[i].IsOutput)
                                                ParametersList[i].Value = cmd.Parameters[ParametersList[i].Name].Value;
                                        }
                                    }

                                    ds.Dispose();
                                }
                            }
                        }
                    }
                }
                catch (SqlException sqlEX)
                {
                    if (sqlEX.Number == (int)RetryableSqlErrors.Timeout)
                    {
                        retryCount++;
                        Thread.Sleep(longWait);
                    }
                    else
                    {
                        throw;
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    if (cmd != null)
                    {
                        cmd.Connection.Close();
                        cmd.Dispose();
                        cmd = null;
                    }
                }
            } while (retryCount > 0 && retryCount < MAX_RETRY);

            return toRet;
        }

        public static List<T> ExecuteSP<T>(string StoredProcedureName, List<Parameter> ParametersList = null, string SortColumn = ""
            , string connectionString = null)
        {
            List<T> toRet = new List<T>();
            var retryCount = 0;
            string connString = !string.IsNullOrEmpty(connectionString) ? connectionString : JCUtils.ConnectionString;

            do
            {
                SqlCommand cmd = null;
                try
                {
                    using (SqlConnection conn = new SqlConnection(connString))
                    {
                        using (cmd = conn.CreateCommand())
                        {
                            cmd.CommandTimeout = conn.ConnectionTimeout;
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.CommandText = StoredProcedureName;

                            if (ParametersList != null && ParametersList.Count > 0)
                                for (int i = 0; i < ParametersList.Count; i++)
                                    if (ParametersList[i] != null)
                                        cmd.Parameters.AddWithValue(ParametersList[i].Name, ParametersList[i].Value);

                            conn.Open();

                            using (SqlDataReader dr = cmd.ExecuteReader())
                                toRet = DataReaderMapToList<T>(dr);
                        }
                    }
                }
                catch (SqlException sqlEX)
                {
                    if (sqlEX.Number == (int)RetryableSqlErrors.Timeout)
                    {
                        retryCount++;
                        Thread.Sleep(JCUtils.longWait);
                    }
                    else
                        throw;
                }
                catch
                {
                    throw;
                }
                finally
                {
                    if (cmd != null)
                    {
                        cmd.Connection.Close();
                        cmd.Dispose();
                        cmd = null;
                    }
                }
            } while (retryCount > 0 && retryCount < MAX_RETRY);

            return toRet;
        }

        private static List<T> DataReaderMapToList<T>(IDataReader dr)
        {
            if (dr == null)
                return null;

            List<T> toRet = new List<T>();
            var obj = Activator.CreateInstance<T>();
            Type objType = obj.GetType();
            System.Reflection.PropertyInfo[] objPropertyInfo = objType.GetProperties();

            try
            {
                while (dr.Read())
                {
                    obj = Activator.CreateInstance<T>();

                    foreach (System.Reflection.PropertyInfo prop in objPropertyInfo)
                        if (!object.Equals(dr[prop.Name], DBNull.Value))
                            prop.SetValue(obj, dr[prop.Name], null);

                    toRet.Add(obj);
                }
            }
            catch
            {
                throw;
            }

            return toRet;
        }

        public static void SendEmail(string Sender, List<string> To, string Subject, string Message, string AttachFileName = null)
        {
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(Sender, "Sender Name");
            mailMessage.Subject = Subject;
            mailMessage.Body = Message;
            mailMessage.IsBodyHtml = true;

            if (!String.IsNullOrWhiteSpace(AttachFileName) && (File.Exists(AttachFileName)))
            {
                Attachment itemAttach = new Attachment(AttachFileName);
                mailMessage.Attachments.Add(itemAttach);
            }

            for (int i = 0; i < To.Count; i++)
                if (!string.IsNullOrEmpty(To[i]))
                    mailMessage.To.Add(new MailAddress(To[i]));

            try
            {
                using (SmtpClient sClient = new SmtpClient("smtpserver"))
                {
                    sClient.Port = 0;
                    sClient.EnableSsl = false;
                    sClient.Credentials = new System.Net.NetworkCredential("SmtpUser", "SmtpPassword");
                    sClient.Send(mailMessage);
                    sClient.Dispose();
                }
            }
            catch
            {
                throw;
            }
        }
    }

    public enum RetryableSqlErrors : int
    {
        Timeout = -2,
        NoLock = 1204,
        Deadlock = 1205,
        WordbreakerTimeout = 30053,
    }
}