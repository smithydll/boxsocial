function showPopupMenu(event, id) {
    if (!$('#' + id).is(":visible")) {
        hideSideBar();
        event.stopPropagation();
        $("#fade").show();
        $('#main-page').css('overflow', 'hidden').css('top', '0').css('bottom', '0').css('position', 'absolute').css('width', '100%');
        $('#' + id).show();
        $('#' + id + ' li').addClass('main');
        $('#' + id).append('<li class="cancel"><a onclick="return hideSideBar();">' + lang['CANCEL'] + '</a></li>');
    }
    return false;
}

function showSideBar(event) {
    if (!$('#boxsocial-menu').is(":visible")) {
        hideSideBar();
        event.stopPropagation();
        $('#boxsocial-menu').show().animate({ left: 0 });
    }
    return false;
}

function showPagesBar(event) {
    if (!$('#pages-menu').is(":visible")) {
        hideSideBar();
        event.stopPropagation();
        $('#pages-menu').show().animate({ right: 0 });
    }
    return false;
}

function hideSideBar() {
    $('#main-page').show().css('overflow', 'auto').css('top', 'auto').css('bottom', 'auto').css('position', 'relative').css('width', 'auto');
    $("#fade").hide();
    if ($('#boxsocial-menu').is(":visible")) {
        $('#boxsocial-menu').animate({ left: -200 }, function () { $(this).hide(); });
    }
    if ($('#pages-menu').is(":visible")) {
        $('#pages-menu').animate({ right: -250 }, function () { $(this).hide(); });
    }
    if ($('#post-menu').is(":visible")) {
        $('input:focus').blur();
        $('#post-menu').hide();
        $('#post-form').trigger("reset");
        checkPhotoUpload();
    }
    if ($('#search-menu').is(":visible")) {
        $('input:focus').blur();
        $('#search-menu').hide();
        $('#search-form').trigger("reset");
    }
    $('.popup-menu').each(function (i) {
        if ($(this).is(":visible")) {
            $(this).hide();
            $(this).children('li.cancel').remove();
        }
    });
    $("ul.popup-menu").hide();
    $('#post-form').trigger("reset");
    $('#upload canvas').remove();
    hideActions();
    return false;
}

function hideActions() {
    $('li.list-item').each(function () {
        var right = 0;
        var tRight = 0;
        $(this).children('.action').each(function () {
            tRight += $(this).outerWidth();
        });
        $(this).children('.action').each(function () {
            var r = right;
            right += $(this).outerWidth();
            if ($(this).is(":visible")) {
                $(this).animate({ right: r - tRight }, function () {
                    $(this).hide();
                });
            }
        });
    });
}

function showPostBar(event) {
    if (!$('#post-menu').is(":visible")) {
        hideSideBar();
        event.stopPropagation();
        $('#post-menu').show();
        $('#post-menu').trigger('click');
        $('#main-page').hide();
    }
    return false;
}

function showSearchBar(event) {
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

$(function () {
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
    $("#fade").bind('click', function () {
        hideSideBar();
    });
});

$(document).ready(function () {
    $(window).on('swiperight', function (event, obj) {
        if (obj.direction.startX < 10) {
            //showSideBar(event);
        }
    });

    $(window).on('swipeleft', function (event, obj) {
        if ($('#boxsocial-menu').is(":visible")) {
            //hideSideBar();
        }
    });
});

//li.list-item

$(document).ready(function () {
    $('li.list-item').on('swiperight', function (event, obj) {
        hideActions();
    });

    $('li.list-item').on('swipeleft', function (event, obj) {
        hideSideBar();
        var right = 0;
        var tRight = 0;
        $(this).children('.action').each(function () {
            tRight += $(this).outerWidth();
        });
        $(this).children('.action').each(function () {
            var r = right;
            $(this).css('right', r - tRight).show();
            right += $(this).outerWidth();
            $(this).animate({ right: r });
        });
    });
});

function checkPhotoUpload() {
    if ($("#fileupload").val() == '') {
        $("#form-module").val('profile');
        $("#form-sub").val('status');
        $("#message").attr('name', 'message');
    } else {
        $("#form-module").val('galleries');
        $("#form-sub").val('upload');
        $("#message").attr('name', 'description');
    }
    return false;
}

var filesList = [];

function submitPost() {
    var status = false;
    $('#status-submit').attr('disabled', 'disabled');
    switch ($("#form-module").val()) {
        case 'profile':
            $("#form-module :input").attr("disabled", true);
            status = SendAction(null, mobileSent);
            break;
        case 'galleries':
            var xhr = $('#fileupload').fileupload('send', { files: filesList }).done(function (result) {
                var r = ProcessAjaxResult(result);
                if (r != null) {
                    hideSideBar();
                    SentAction(r);
                    filesList = [];
                    $('#upload canvas').remove();
                }
                $("#form-module :input").attr("disabled", false);
            });
            $("#form-module :input").attr("disabled", true);
            break;
    }
    return status;
}

function mobileSent(r, e, a) {
    hideSideBar();
    $("#form-module :input").attr("disabled", false);
    SentAction(r, e, a);
}

$(function () {
    /*.hide()*/
    $('#fileupload').fileupload({
        disableImageResize: /Android(?!.*Chrome)|Opera/
            .test(window.navigator.userAgent),
        imageMaxWidth: 2560,
        imageMaxHeight: 2560,
        previewMaxWidth: 100,
        previewMaxHeight: 100,
        previewCrop: true,
        acceptFileTypes: /(\.|\/)(gif|jpe?g|png)$/i,
        autoUpload: false
    }).on('fileuploadadd', function (e, data) {
        $.each(data.files, function (index, file) {
            filesList.push(file);
        });
    }).on('fileuploadprocessalways', function (e, data) {
        var file = data.files[data.index];
        if (file.preview) {
            $("#upload").append(file.preview);
        }
    });

    $("#form-module").append('<input type="hidden" name="ajax" value="true" />');
});

$(function () {
    $('.user-droplist .textbox, .permission-group-droplist .textbox').hide();
});

function hideUsersBar() {
    if (!$('#post-menu').is(":visible")) {
        $('#main-page').show();
    }
    if ($('#users-menu').is(":visible")) {
        $('input:focus').blur();
        $('#users-menu').hide();
        $('#users-menu').remove();
    }
    return false;
}

function showUsersBar(event, id, mode) {
    var uri = ((mode == 'users') ? 'api/friends' : 'api/acl/get-groups');
    var listClass = ((mode == 'users') ? 'user-droplist' : 'permission-group-droplist');
    if (!$('#users-menu').is(":visible")) {
        event.stopPropagation();
        $('#post-menu').after('<div id="users-menu" class="side-menu bottom-menu">');
        $('#users-menu').show().append('<div class="action-bar"><button onclick="return hideUsersBar();" style="float: right;">' + lang['BACK'] + '</button></div><div class="popup-content"><div id="users-selected" class="' + listClass + '"><input type="text" id="' + id + '-search" /></div><ul id="users-list"></ul></div>');
        $('#' + id + '-search').before($('#' + id).children('span.group, span.username').clone());
        PostToPage(namesRequested, uri, $('#users-list'), { ajax: "true", "name-field": '' }, { 'mode': mode, 'id': id });

        $('#' + id + '-search').bind("keyup", function (event) {
            PostToPage(namesRequested, uri, $('#users-list'), { ajax: "true", "name-field": $(this).val() }, { 'mode': mode, 'id': id });
        });

        $('#users-menu').trigger('click');
        $('#main-page').hide();
    }
    return false;
}

function namesRequested(r, e, a) {
    //e(r);
    var i;
    e.empty();
    for (i = 0; i < r.length; i++) {
        var item = r[i];
        var iid = ((a.mode == 'users') ? item.id : item.typeId + '-' + item.id);

        e.append('<li id="item-' + iid + '" class="droplist-' + ((item.id > 0) ? 'user' : 'group') + '"><a>' + ((item.tile != '') ? '<img src="' + item.tile + '" />' : '') + item.value + '</a></li>')
        $('#item-' + iid).bind('click', { 'item': item }, function (event) {
            var item = event.data.item;
            var iid = ((a.mode == 'users') ? item.id : item.typeId + '-' + item.id);
            if (cv($('#' + a.id + '-ids'), iid) == 0) {
                $('#' + a.id + '-search').before($('<span class="item-' + iid + ' ' + ((item.id > 0) ? 'username' : 'group') + '">' + item.value + '</span>'));
                $('#' + a.id + '-text').before($('<span class="item-' + iid + ' ' + ((item.id > 0) ? 'username' : 'group') + '">' + item.value + '</span>'));
                avl($('#' + a.id + '-ids'), iid);
                nameChecked(item, a.id, a.mode);
            }
        });

        if (cv($('#' + a.id + '-ids'), iid) != 0) {
            nameChecked(item, a.id, a.mode);
        }
    }
}


function nameChecked(item, id, mode) {
    var iid = ((mode == 'users') ? item.id : item.typeId + '-' + item.id);
    $('#item-' + iid + ' > a').append('<a class="check checked">x</a>');
    $('#item-' + iid + ' a.checked').bind('click', { 'item': item }, function (event) {
        event.stopPropagation();
        var item = event.data.item;
        rvl($('#' + id + '-ids'), iid);
        $('.item-' + iid).remove();
        $(this).remove();
    });
}