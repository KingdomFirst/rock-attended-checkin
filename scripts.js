var loadCSSStyles = function () {
    var relPath = '../plugins/cc_newspring/attendedcheckin/Styles/styles.min.css';
    var styleLink = $('<link>').attr('rel', 'stylesheet').attr('href', relPath);
    $('head').append(styleLink);
}();