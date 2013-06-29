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
        $('#pages-menu').animate({ right: -200 }, function () { $(this).hide(); });
    }
    if ($('#post-menu').is(":visible")) {
        if ($('#post-menu').is(':visible')) $('#post-menu').animate({ bottom: -200 }, function () { $(this).hide(); });
    }
    if ($('#search-menu').is(":visible")) {
        if ($('#search-menu').is(':visible')) $('#search-menu').animate({ bottom: -100 }, function () { $(this).hide(); });
    }
}

function showPostBar() {
    if (!$('#post-menu').is(":visible")) {
        hideSideBar();
        event.stopPropagation();
        $('#post-menu').show().animate({ bottom: 0 });
        $('#message').outerWidth($('#post-menu').width() - $('#status-submit').outerWidth(true));
        $('#post-menu').trigger('click');
    }
    return false;
}

function showSearchBar() {
    if (!$('#search-menu').is(":visible")) {
        hideSideBar();
        event.stopPropagation();
        $('#search-menu').show().animate({ bottom: 0 });
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
});

/*$(document).ready(function () {
    $(window).on('swipeone swiperightup', function (event, obj) {
        $('#boxsocial-menu').animate({ left: 0 });
    });

    $(window).on('swipeone swipeleft', function (event, obj) {
        $('#boxsocial-menu').animate({ left: -200 });
    });
});*/