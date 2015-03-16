var AttendedCheckin = (function () {
    var _previousDOB = '';

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

    var calculateAge = function (birthday) {
        var ageDifMs = Date.now() - birthday.getTime();
        var ageDate = new Date(ageDifMs);
        return ageDate.getUTCFullYear() - 1970;
    };

    var showAgeOnBirthdatePicker = function () {
        $('body').on('change', '[data-show-age=true]', function (event) {
            var input = $(this);
            var newVal = input.val();

            if (_previousDOB !== newVal) {
                _previousDOB = newVal;

                if (newVal === '') {
                    input.next("span").find("i").text('').addClass("fa-calendar");
                    return;
                }

                var birthDate = new Date(newVal);
                var age = calculateAge(birthDate);

                var iTag = input.next("span").find("i");
                iTag.text(age).removeClass("fa-calendar");

                if (age < 0) {
                    iTag.css('color', '#f00');
                }
                else {
                    iTag.css('color', 'inherit');
                }
            }
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
            showAgeOnBirthdatePicker();
        }
    };
})();

$(document).ready(AttendedCheckin.init);