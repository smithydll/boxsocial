var host = "/";
var dtp = Array(); /* Date Time Pickers */
var usb = Array(); /* User Select Boxes */
var nid = 0; /* newest ID */

$(document).ready(function () {
    $(".date-drop").hide();
    $(".date-exp").show();
    $(".date-mode").val('ajax');
});

// Append to Value List
function avl(e, i) {
    var l = e.val().split(',');
    for (j in l) {
        if (l[j] == i) {
            return false;
        }
    }
    if (e.val() == '') {
        e.val(i);
    }
    else {
        e.val(e.val() + ',' + i);
    }
    return true;
}

// Remove from Value List
function rvl(e, i) {
    var l = e.val().split(',');
    var m = Array();
    for (j in l) {
        if (l[j] != i) {
            m.push(l[j]);
        }
    }
    e.val(m.join(','));
}

// count Values
function cv(e, i) {
    var l = e.val().split(',');
    var c = 0;
    for (j in l) {
        if (l[j] == i) {
            c++;
        }
    }
    return c;
}

/*
 *Functions
 */

function LikeItem(itemId, itemType, node) {
    return PostToPage(ItemLiked, "api/like?ajax=true&like=like&item=" + itemId + "&type=" + itemType, node, null, null);
}

function DislikeItem(itemId, itemType, node) {
    return PostToPage(ItemLiked, "api/like?ajax=true&like=dislike&item=" + itemId + "&type=" + itemType, node, null, null);
}

function ItemLiked(r, e) {
    if (e.text() == '') {
        e.text(' 1');
    } else {
        e.text(' ' + (parseInt($.trim(e.text())) + 1));
    }
}

function SendStatus(u, f) {
    f = typeof f !== 'undefined' ? f : SentStatus;
    return PostToAccount(f, "profile", "status", -1, { message: $('#message').val(), 'permissions-ids': $('#permissions-ids').val(), 'permissions-text': $('#permissions-text').val() }, u);
}

function SentStatus(r, e, a) {
    $('#status-form').hide();
    if (r['update'] == 'true') {
        $('#status-message').show().text(r['message']).html((a != null ? a + ' ' : '') + $('#status-message').show().text() + ' <em>Updated a second ago</em>');
        $('.status-feed').prepend(r['template']);
    }
}

function SendAction(u, f) {
    f = typeof f !== 'undefined' ? f : SentAction;
    return PostToAccount(f, "profile", "status", -1, { message: $('#message').val(), action: 'true', 'permissions-ids': $('#permissions-ids').val(), 'permissions-text': $('#permissions-text').val() }, u);
}

function SentAction(r, e, a) {
    $('#status-form').trigger("reset");
    $("#permissions-ids").val("");
    $("#permissions-text").removeAttr("style");
    $("#permissions").children(".group, .username").remove();
    $("#permissions").children(".empty").show();
    if (r['update'] == 'true') {
        $('.today-feed ul.feed-list').first().before(r['template']);
    }
    if (r['newest-id'] > 0) {
        nid = r['newest-id'];
    }
}

function DeleteComment(id, iid) {
    return PostToPage(DeletedComment, "api/comment?mode=delete&item=" + id, "#c" + id, { ajax: "true" });
}

function DeletedComment(r, e) {
    var height = $(e).height();
    $(e).children().fadeOut(500, function () {
        $(this).parent().html('<p>Comment has been successfully deleted.</p>').height(height).css('background-color', '#ffdddd').delay(2000).animate({ height: '0' }, 500, 'linear', function () {
            $(this).hide();
        });
    }
    );

    $('.comment-count').text(parseInt($('.comment-count').text()[0]) - 1);
}

function ReportComment(id, iid) {
    return PostToPage(null, "api/comment/?mode=report&ajax=true&item=" + id, null, { ajax: "true" });
}

var ciid;
function QuoteComment(id, iid) {
    ciid = iid;
    return PostToPage(QuotedComment, "api/comment?ajax=true&mode=fetch&item=" + id, null, { ajax: "true" });
}

function QuotedComment(r, e) {
    $("#comment-text-" + ciid).val(r['message']).focus();
}

var csort;
var cid;
function SubmitComment(id, type, zero, sort, text) {
    csort = sort;
    cid = id;
    if (text == null) {
        text = $("#comment-text-" + id).val();
    }
    return PostToPage(SubmitedComment, "api/comment", $('.comments-for-' + type + '-' + id), { ajax: "true", item: id, type: type, comment: text });
}

function SubmitedComment(r, e) {
    var nli = $('<div>').html(r['message']);
    var n = e.children(".comment-list");
    if (csort == 'desc' && n.children().length > 0) {
        nli.insertBefore(n.children()[0]);
    }
    else {
        e.children(".comment-list").append(nli);
    }
    e.children(".comment-form").hide();

    e.find(".comment-submit-" + cid).attr("disabled", "disabled").val("Comment Posted.");
    e.find(".comment-text-" + cid).attr("disabled", "disabled");

    e.find(".no-comments").hide();

    e.find(".comment-count").text(parseInt(e.find(".comment-count").text()[0]) + 1);
}

function SubscribeItem(id, type, unsubscribe) {
    var mode = 'subscribe';
    if (unsubscribe) mode = 'unsubscribe';
    return PostToPage(SubscribedItem, "api/subscribe", null, { ajax: "true", item: id, type: type, mode: mode }, { item: id, type: type });
}

function SubscribedItem(r, e, a) {
    var s = $(".subscribe-" + a['type'] + '-' + a['item']);
    var c = s.children("a");
    var t = s.next('span').eq(0).text();
    var i = parseInt(t);
    if (i == t) { // If the number ends in k or M then don't bother to increment/decrement
        if (s.toggleClass("subscribed").hasClass("subscribed")) {
            c.text('Unsubscribe');
            s.next('span').text(i + 1);
        } else {
            c.text('Subscribe');
            s.next('span').text(i - 1);
        }
    }
}

function SubmitListItem(id) {
    var text = $("#text").val();
    return PostToAccount(SubmitedListItem, "pages", "lists", id, { mode: "append", save: "Add", text: text });
}

function SubmitedListItem(r, e) {
    var nli = $('<li>').html(r['message']);
    $("#list-list").append(nli);

    $("#no-list-items").hide();
    $("#text").val('');
}

function LoadComments(id, type, node) {
    return PostToPage(LoadedComments, "api/comment?ajax=true&mode=load&item=" + id + "&type=" + type, node, { ajax: "true" });
}

function LoadedComments(r, e) {
    e.show();
    e.html(r['message']);
}

function PostToAccount(onPost, module, sub, id, params, a) {
    var par = { module: module, sub: sub, id: id };
    if (params != null) {
        return PostToPage(onPost, "account/?ajax=true", null, $.extend(par, params), a);
    }
    else {
        return PostToPage(onPost, "account/?ajax=true", null, par, a);
    }
}

function PostToPage(onPost, page, nodes, params, a) {
    var u = page;
    if (page.indexOf(host) != 0) {
        u = host + page;
    }
    $.post(AppendSid(u), params, function (data) {
        if (onPost != null) {
            var r = ProcessAjaxResult(data);
            if (r != null) onPost(r, nodes, a);
        }
        else {
            ProcessAjaxResult(data);
        }
    }, 'xml');

    return false;
}

function AppendSid(uri) {
    if (uri.indexOf("?") >= 0) {
        uri = uri + "&";
    }
    else {
        uri = uri + "?";
    }
    return uri + "sid=" + sid;
}

function ProcessAjaxResult(doc) {
    var type = GetNode(doc, 'type');
    var status;
    var title;
    var message;

    if (type == 'Message') {
        //window.alert(GetNode(doc, 'title'), GetNode(doc, 'message'));
        $(function () {
            $("#dialog-message").attr('title', GetNode(doc, 'title')).text(GetNode(doc, 'message')).dialog({
                modal: true,
                resizable: false,
                buttons: {
                    Ok: function () {
                        $(this).dialog("close");
                    }
                }
            });
        });
        return null;
    }
    else if (type == 'Raw') {
        return { code: GetNode(doc, 'code'), message: GetNode(doc, 'message') };
    }
    else if (type == 'Status') {
        return { code: GetNode(doc, 'code') };
    }
    else if (type == 'Array') {
        return { code: GetNode(doc, 'code'), 'array': GetNode(doc, 'array') };
    }
    else if (type == 'Dictionary') {
        var a = {};
        var xmlDoc = $.parseXML(doc);
        var xml = $(doc);
        var e = xml.find('array').find('item').each(function () {
            //a.push({ key: $(this).find('key').text(), value: $(this).find('value').text() });
            a[$(this).find('key').text()] = $(this).find('value').text();
        });
        return a;
    }
    else if (type == 'UserDictionary') {
        var a = new Array();
        var xmlDoc = $.parseXML(doc);
        var xml = $(doc);
        var e = xml.find('array').find('item').each(function () {
            a.push({ id: $(this).find('id').text(), value: $(this).find('value').text(), tile: $(this).find('tile').text() });
        });
        return a;
    }
    else if (type == 'PermissionGroupDictionary') {
        var a = new Array();
        var xmlDoc = $.parseXML(doc);
        var xml = $(doc);
        var e = xml.find('array').find('item').each(function () {
            a.push({ id: $(this).find('id').text(), typeId: $(this).find('type-id').text(), value: $(this).find('value').text(), tile: $(this).find('tile').text() });
        });
        return a;
    }
    else {
        return doc;
    }
}

function GetNode(doc, node) {
    var n = doc.getElementsByTagName(node)[0];
    return n.text || n.textContent;
}

function UpdateSlug() {
    var aeiou = "aáäàâeéëèêiíïìîoóöòôuúüùû";
    var rogex = /([\W]+)/g;
    var slug = $("#title").val().toLowerCase();

    var i, j;
    for (i = 0; i < 25; i += 5) {
        for (j = 1; j < 5; j++) {
            slug = slug.replace(aeiou.charAt(i + j), aeiou.charAt(i));
        }
    }
    slug = slug.replace(rogex, '-');

    $("#slug").val(slug);
}

function ShowSpamComment(id) {
    var commentDiv = $("#comment-" + id);
    var commentADiv = $("#comment-a-" + id);
    if (commentDiv.style.display == "block") {
        // hide
        commentDiv.style.display = "none";
        commentADiv.val("show comment");
    }
    else {
        // show
        commentDiv.style.display = "block";
        commentADiv.val("hide comment");
    }

    return false;
}

function EnableDateTimePickers() {
    for (i in dtp) {
        $('#' + dtp[i][0]).hide();
        $('#' + dtp[i][1]).show();
    }
}

function ParseDatePicker(n, m) {
    var el = $('#' + n);

    return PostToPage(SubmitedDate, "api/functions", el, { ajax: "true", fun: "date", date: el.val(), medium: m });
}

function ParseTimePicker(n) {
    var el = $('#' + n);

    return PostToPage(SubmitedTime, "api/functions", el, { ajax: "true", fun: "time", time: el.val() });
}

function SubmitedDate(r, e) {
    e.val(r['message']);
}

function SubmitedTime(r, e) {
    e.val(r['message']);
}

function ToggleAdvanced() {
    if ($("#show-advanced").hasClass('hidden')) {
        $(".advanced-field").show();
        $("#show-advanced span").text('‹');
    }
    else {
        $(".advanced-field").hide();
        $("#show-advanced span").text('›');
    }
    $("#show-advanced").toggleClass('hidden shown');

    return false;
}

function DeleteStatus(i) {
    return PostToAccount(DeletedStatus, "profile", "status", -1, { mode: "delete", id: i }, i);
}

function DeletedStatus(r, e, a) {
    $("#status-" + a).remove();
}

function ShowShareContent(r, e, a) {
    $("#share-form").html(r['message']);
    preparePermissionsList('#share-permissions');
    $("#share-form").dialog({
        modal: true,
        width: 640,
        resizable: false,
        buttons: {
            Cancel: function () {
                $(this).dialog("close");
            },
            Share: function () {
                PostToPage(SharedContent, "api/share", null, { ajax: 'true', 'share-message': $('#share-message').val(), item: a['item'], type: a['type'], 'share-permissions-ids': $('#share-permissions-ids').val(), 'share-permissions-text': $('#share-permissions-text').val() }, '');
                $(this).dialog("close");
                $("#share-form").html("Post shared");
                var successDialog = $("#share-form").dialog({ buttons: { OK: function () { $(this).dialog("close"); } } });
                setTimeout(function () { successDialog.dialog("close") }, 2000);
            }
        }
    });
}

function SharedContent(r, e, a) {
    SentStatus(r, e, a);
}

var lastScrollPosn = 0;
var infiniteLoading = false;
var loadCount = 0;

$(document).ready(function () {
    $(".infinite-more").siblings().hide();
    $(".infinite-more").show();
    $(window).scroll(function () {
        var trigger = 200;

        if (infiniteLoading == false && loadCount < 2) {
            if ($(window).scrollTop() != lastScrollPosn) {
                console.log($(window).scrollTop());
                $(".infinite").each(function () {
                    if ($(window).scrollTop() + $(window).height() > ($(this).offset().top + $(this).height() - trigger)) {
                        if ($(this).children("p").children(".infinite-more").is(":visible")) {
                            infiniteLoading = true;
                            $(this).children("p").children(".infinite-more").trigger('click');
                        }
                    }
                });
                lastScrollPosn = $(window).scrollTop();
            }
        }
    });
});

function loadInfiniteContent(u, n) {

    PostToPage(LoadedInfinite, u, n, { ajax: 'true' }, '');
    return false;
}

function LoadedInfinite(r, e, a) {
    e.append(r['message']);

    var c = r['code'];
    var more = $('.infinite-more');
    if (c == 'noMoreContent') {
        more.remove();
    }
    else {
        more.attr('onclick', "return false;");
        more.unbind('click');
        more.bind('click', function () {
            return loadInfiniteContent(c, e);
        });
    }

    loadCount++;
    infiniteLoading = false;
}

function loadNewContent(u, n) {
    PostToPage(LoadedNew, u, n, { ajax: 'true' }, '');
    return false;
}

function LoadedNew(r, e, a) {
    e.prepend(r['message']);

    var c = r['code'];
    if (c == 'noNewContent') {
    }
    else {
    }
}

function toggleStatusComments(parent, id, type, el) {
    if (parent.hasClass('active')) {
        parent.removeClass('active');
        el.html('');
    } else {
        LoadComments(id, type, el);
        parent.addClass('active');
    }
    return false;
}
