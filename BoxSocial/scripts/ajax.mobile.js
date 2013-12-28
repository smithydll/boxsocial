function showSideBar() {
    if (!$('#boxsocial-menu').is(":visible")) {
        hideSideBar();
        event.stopPropagation();
        $('#boxsocial-menu').show().animate({ left: 0 });
    }
    return false;
}

function showPagesBar() {
    if (!$('#pages-menu').is(":visible")) {
        hideSideBar();
        event.stopPropagation();
        $('#pages-menu').show().animate({ right: 0 });
    }
    return false;
}

function hideSideBar() {
    if ($('#boxsocial-menu').is(":visible")) {
        $('#boxsocial-menu').animate({ left: -200 }, function () { $(this).hide(); });
    }
    if ($('#pages-menu').is(":visible")) {
        $('#pages-menu').animate({ right: -250 }, function () { $(this).hide(); });
    }
    if ($('#post-menu').is(":visible")) {
        if ($('#post-menu').is(':visible')) $('#post-menu').animate({ bottom: '-64pt' }, function () { $(this).hide(); }).removeClass('iosfixed');
    }
    if ($('#search-menu').is(":visible")) {
        if ($('#search-menu').is(':visible')) $('#search-menu').animate({ bottom: '-32pt' }, function () { $(this).hide(); }).removeClass('iosfixed');
    }
}

function showPostBar() {
    if (!$('#post-menu').is(":visible")) {
        hideSideBar();
        event.stopPropagation();
        $('#post-menu').addClass('iosfixed').show().css('bottom', 0);
        $('#message').outerWidth($('#post-menu').width() - $('#status-submit').outerWidth(true));
        $('#post-menu').trigger('click');
    }
    return false;
}

function showSearchBar() {
    if (!$('#search-menu').is(":visible")) {
        hideSideBar();
        event.stopPropagation();
        $('#search-menu').addClass('iosfixed').show().css('bottom', 0);
        $('#search-text').outerWidth($('#search-menu').width() - $('#search-menu input[type="submit"]').outerWidth(true));
        $('#search-menu').trigger('click');
    }
    return false;
}

$(document).ready(function () {
    $('#post-menu').click(function () {
        $("#message").focus();
    });
    $('#search-menu').click(function () {
        $("#search-text").focus();
    });

    if (parent.location.hash == '#boxsocial-menu') {
        $('#boxsocial-menu').show().css('left', 0);
    }
});

/*$(document).ready(function () {
    $(window).on('swipeone swiperightup', function (event, obj) {
        $('#boxsocial-menu').animate({ left: 0 });
    });

    $(window).on('swipeone swipeleft', function (event, obj) {
        $('#boxsocial-menu').animate({ left: -200 });
    });
});*/