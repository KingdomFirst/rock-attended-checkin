var loadCSSStyles = function () {
    var relPath = '../plugins/cc_newspring/attendedcheckin/Styles/styles.min.css';
    var styleLink = $('<link>').attr('rel', 'stylesheet').attr('href', relPath);
    $('head').append(styleLink);
}();

var setFormEvents = function () {
    var setFocus = function () {
        $('.btn').blur();
        $('input[type=text]').first().focus();
    };

    var calculateAge = function (birthday) {
        var ageDifMs = Date.now() - birthday.getTime();
        var ageDate = new Date(ageDifMs);
        return ageDate.getUTCFullYear() - 1970;
    };

    var _previousDOB = '';
    var showAgeOnBirthdatePicker = function () {
        $('body').on('change', '[data-show-age=true]', function () {
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

    return {
        init: function () {
            setFocus();
            showAgeOnBirthdatePicker();
        }
    };
}();

$(document).ready(setFormEvents.init);