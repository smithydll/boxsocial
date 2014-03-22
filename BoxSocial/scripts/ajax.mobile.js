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
    $('#main-page').show();
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
        if ($('#search-menu').is(':visible')) $('#search-menu').animate({ bottom: '-32pt' }, function () { $(this).hide(); });
    }
    $('#post-form').trigger("reset");
    $('#upload canvas').remove();
    return false;
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