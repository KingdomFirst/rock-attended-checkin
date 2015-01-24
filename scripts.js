var AttendedCheckin = (function () {

    var loadStyles = function () {
        var relPath = '../plugins/cc_newspring/attendedcheckin/styles.css';
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

    var hasOverFlow = function (el) {
        var curOverflow = el.style.overflow;

        if (!curOverflow || curOverflow === 'visible') {
            el.style.overflow = 'hidden';
        }

        var isOverflowing = el.clientWidth < el.scrollWidth || el.clientHeight < el.scrollHeight;
        el.style.overflow = curOverflow;

        return isOverflowing;
    };

    var addShadowIfOverflows = function (element) {
        if (!hasOverFlow(element)) {
            $(element).css('box-shadow', 'none');
            return;
        }

        var bottomShadowCss = 'inset 0 -15px 15px #999999';
        $(element).css('box-shadow', bottomShadowCss);
    };

    var handleGridOverflowShadows = function () {
        var grids = $('.grid');
        var hasGrids = grids.length > 0;

        if (!hasGrids) {
            return;
        }

        var handleGrids = function () {
            for (var i = 0 ; i < grids.length; i++) {
                addShadowIfOverflows(grids.get(i));
            }
        };

        setTimeout(handleGrids, 100);
        $(window).resize(handleGrids);
    };

    return {
        init: function () {
            loadStyles();
            fixDatePickerZIndex();
            fixFocus();
            handleGridOverflowShadows();
        }
    };
})();

$(document).ready(AttendedCheckin.init);