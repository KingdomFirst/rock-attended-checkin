// Add css file to page
$('head').append('<link rel="stylesheet" type="text/css" href="../plugins/cc_newspring/attendedcheckin/styles.css" />');

// The date picker is getting lost behind other elements and the z-index is set by js in core
$('body').on('focus', '.date-picker', function () {
    setTimeout(function () {
        $('.datepicker').css('z-index', 2000);
    }, 100);
});

$(document).ready(function () {
    // Remove focus from buttons on load - this was triggering :active css set in core
    $('.btn').blur();

    // Set focus to first textbox when page loads for convenience
    $('input[type=text]').first().focus();
});