function showSideBar() {
    hideSideBar();
    event.stopPropagation();
    $('#boxsocial-menu').animate({ left: 0 });
    return false;
}

function showPagesBar() {
    hideSideBar();
    event.stopPropagation();
    $('#pages-menu').animate({ right: 0 });
    return false;
}

function hideSideBar() {
    $('#boxsocial-menu').animate({ left: -200 });
    $('#pages-menu').animate({ right: -200 });
    if ($('#post-menu').is(':visible')) $('#post-menu').animate({ bottom: -200 }, function () { $(this).hide(); });
    if ($('#search-menu').is(':visible')) $('#search-menu').animate({ bottom: -100 }, function () { $(this).hide(); });
}

function showPostBar() {
    hideSideBar();
    event.stopPropagation();
    $('#post-menu').show().animate({ bottom: 0 });
    return false;
}

function showSearchBar() {
    hideSideBar();
    event.stopPropagation();
    $('#search-menu').show().animate({ bottom: 0 });
    return false;
}

/*$(document).ready(function () {
    $(window).on('swipeone swiperightup', function (event, obj) {
        $('#boxsocial-menu').animate({ left: 0 });
    });

    $(window).on('swipeone swipeleft', function (event, obj) {
        $('#boxsocial-menu').animate({ left: -200 });
    });
});*/