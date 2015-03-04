var AttendedCheckin = (function () {

    var loadStyles = function () {
        var relPath = '../plugins/cc_newspring/attendedcheckin/Styles/styles.css';
        var styleLink = $('<link>').attr('rel', 'stylesheet').attr('href', relPath);
        $('head').append(styleLink);
    };

    var fixDatePickerZIndex = function () {
        // The date picker is getting lost behind other elements and the z-index is set by js in core
        $('body').on('focus', '.date-picker', function () {
            setTimeout(function () {
                $('.datepicker').css('z-index', 2000);
            }, 100);
        });
    };

    var fixFocus = function () {
        $('.btn').blur();
        $('input[type=text]').first().focus();
    };

    return {
        init: function () {
            loadStyles();
            fixDatePickerZIndex();
            fixFocus();    
        }
    };
})();

$(document).ready(AttendedCheckin.init);