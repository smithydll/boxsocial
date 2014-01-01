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
        $('#post-menu').hide();
    }
    if ($('#search-menu').is(":visible")) {
        if ($('#search-menu').is(':visible')) $('#search-menu').animate({ bottom: '-32pt' }, function () { $(this).hide(); });
    }
    return false;
}

function showPostBar() {
    if (!$('#post-menu').is(":visible")) {
        hideSideBar();
        event.stopPropagation();
        $('#post-menu').show();
        $('#post-menu').trigger('click');
    }
    return false;
}

function showSearchBar() {
    if (!$('#search-menu').is(":visible")) {
        hideSideBar();
        event.stopPropagation();
        $('#search-menu').show().css('bottom', 0);
        $('#search-text').outerWidth($('#search-menu').width() - $('#search-menu input[type="submit"]').outerWidth(true));
        $('#search-menu').trigger('click');
    }
    return false;
}

var currentScrollPosition = 0;

$(document).ready(function () {
    $('#post-menu').click(function () {
        currentScrollPosition = $(document).scrollTop();
        $("#message").focus();
        $(document).scrollTop(currentScrollPosition);
    });
    $('#search-menu').click(function () {
        currentScrollPosition = $(document).scrollTop();
        $("#search-text").focus();
        $(document).scrollTop(currentScrollPosition);
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