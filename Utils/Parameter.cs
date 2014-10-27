using System.Data;
namespace JCB_Utils
{
    [System.Serializable]
    public class Parameter
    {
        public string Name { get; set; }
        public dynamic Value { get; set; }
        public bool IsOutput { get; set; }
        public SqlDbType SQLType { get; set; }
    }
}