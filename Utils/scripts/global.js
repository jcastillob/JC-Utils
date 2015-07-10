function FormatPhoneNumberOnKeyPress(s, e) {
    var keyCode = ((e.htmlEvent.which) ? e.htmlEvent.which : event.keyCode);

    if (OnlyInteger(keyCode)) {
        //don't try to format when backspace, arrows, delete are pressed
        if (keyCode != 37 && keyCode != 39 && keyCode != 8 && keyCode != 46) {
            var number = "INPUT VALUE";//get the inputs value ;

            var stripped = number.replace(/[^\d]+/g, '');
            var toRet = '';

            switch (stripped.length) {
                case 0:
                case 1:
                case 2:
                    toRet = stripped;
                    break;
                case 3:
                case 4:
                case 5:
                    toRet = stripped.replace(/(\d{3})/, '($1) ');
                    break;
                case 6:
                case 7:
                case 8:
                    toRet = stripped.replace(/(\d{3})(\d{3})/, '($1) $2-');
                    break;
                case 9:
                    toRet = stripped.replace(/(\d{3})(\d{3})(\d{3})/, '($1) $2-$3');
                    break;
                case 10:
                    toRet = stripped.replace(/(\d{3})(\d{3})(\d{4})/, '($1) $2-$3');
                    break;
                case 11:
                    //(123) 567-8900 x1
                    toRet = stripped.replace(/(\d{3})(\d{3})(\d{4})(\d{1})/, '($1) $2-$3 x$4');
                    break;
                case 12:
                    //(123) 567-8900 x12
                    toRet = stripped.replace(/(\d{3})(\d{3})(\d{4})(\d{2})/, '($1) $2-$3 x$4');
                    break;
                case 13:
                    //(123) 567-8900 x123
                    toRet = stripped.replace(/(\d{3})(\d{3})(\d{4})(\d{3})/, '($1) $2-$3 x$4');
                    break;
                case 14:
                    //(123) 567-8900 x1234
                    toRet = stripped.replace(/(\d{3})(\d{3})(\d{4})(\d{4})/, '($1) $2-$3 x$4');
                    break;
                case 14:
                    //(123) 567-8900 x12345
                    toRet = stripped.replace(/(\d{3})(\d{3})(\d{4})(\d{5})/, '($1) $2-$3 x$4');
                    break;
                default:
                    toRet = stripped;
                    break;
            }

            //toRet : has the formatted value 
            return true;
        }
    } else {
        return false;
    }
}

function OnlyInteger(charCode) {
    if ((charCode == 188 || charCode == 189 || charCode == 109 || charCode == 110) ||
        (charCode == 37 || charCode == 39 || charCode == 8 || charCode == 46) ||
        (charCode >= 48 && charCode <= 57) ||
        (charCode >= 96 && charCode <= 105)) {
        return true;
    } else
        return false;
}