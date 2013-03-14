var host = "/";
var dtp = Array(); /* Date Time Pickers */
var usb = Array(); /* User Select Boxes */

// Append to Value List
function avl(e, i) {
	var l = e.val().split(',');
	for (j in l)
	{
		if (l[j] == i)
		{
			return false;
		}
	}
	if (e.val() == '')
	{
		e.val(i);
	}
	else
	{
		e.val(e.val() + ',' + i);
	}
	return true;
}

// Remove from Value List
function rvl(e, i) {
	var l = e.val().split(',');
	var m = Array();
	for (j in l)
	{
		if (l[j] != i)
		{
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

function StarOver(stars, itemId, itemType) {
	var i = 1;
	for (i = 1; i <= stars; i++)
	{
		$("#rate-" + i + "s-" + itemId + "-" + itemType).attr("src", "/images/star-on.png");
	}
	for (i = stars + 1; i <= 5; i++)
	{
		var itemName = "#rate-" + i + "s-" + itemId + "-" + itemType;
		if ($(itemName).hasClass("rank-on"))
		{
			$(itemName).attr("src", "/images/star-on.png");
		}
		else
		{
			$(itemName).attr("src", "/images/star-off.png");
		}
	}
}

function SubmitRating(stars, itemId, itemType) {
	return PostToPage(null, "api/rate?ajax=true&rating=" + stars + "&item=" + itemId + "&type=" + itemType, null, null, null);
}

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

function ShareItem(itemId, itemType, node) {
    return false;
}

function ItemShared(r, e) {
}

function SendStatus(u) {
    return PostToAccount(SentStatus, "profile", "status", -1, { message: $('#message').val(), 'permissions-ids': $('#permissions-ids').val(), 'permissions-text': $('#permissions-text').val() }, u);
}

function SentStatus(r, e, a) {
	$('#status-form').hide();
	$('#status-message').show().text(r['message']).html((a != null ? a + ' ' : '') + $('#status-message').show().text() + ' <em>Updated a second ago</em>');
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
    return PostToPage(null, "api/comment/?mode=report&ajax=true&item=" + id, null, { ajax: "true" } );
}

var ciid;
function QuoteComment(id, iid) {
	ciid = iid;
	return PostToPage(QuotedComment, "api/comment?ajax=true&mode=fetch&item=" + id, null, { ajax: "true" } );
}

function QuotedComment(r, e) {
    $("#comment-text-" + ciid).val(r['message']).focus();
}

var csort;
var cid;
function SubmitComment(id, type, zero, sort, text)
{
	csort = sort;
	cid = id;
	if (text == null) {
	    text = $("#comment-text-" + id).val();
	}
	return PostToPage(SubmitedComment, "api/comment", $('.comments-for-' + type + '-' + id), { ajax: "true", item: id, type: type, comment: text } );
}

function SubmitedComment(r, e) {
    var nli = $('<div>').html(r['message']);
    var n = e.children(".comment-list");
	if (csort == 'desc' && n.children().length > 0)
	{
	    nli.insertBefore(n.children()[0]);
	}
	else
	{
		e.children(".comment-list").append(nli);
	}
    e.children(".comment-form").hide();

    e.find(".comment-submit-" + cid).attr("disabled", "disabled").val("Comment Posted.");
    e.find(".comment-text-" + cid).attr("disabled", "disabled");

    e.find(".no-comments").hide();

    e.find(".comment-count").text(parseInt(e.find(".comment-count").text()[0]) + 1);
}

function SubmitListItem(id) {
	var text = $("text").val();
	return PostToAccount(SubmitedListItem, "pages", "lists", id, { mode: "append", save: "Add", text: text } );
}

function SubmitedListItem(r, e) {
	var nli = $('<li>').html(r['message']);
	$("#list-list").append(nli);

	$("#no-list-items").hide();
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
	if (params != null)
	{
		return PostToPage(onPost, "account/?ajax=true", null, $.extend(par, params), a);
	}
	else
	{
		return PostToPage(onPost, "account/?ajax=true", null, par, a);
	}
}

function PostToPage(onPost, page, nodes, params, a) {
    $.post(AppendSid(host + page), params, function (data) {
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
   if (uri.indexOf("?") >= 0)
   {
   	uri = uri + "&";
   }
   else
   {
   	uri = uri + "?";
   }
   return uri + "sid=" + sid;
}

function ProcessAjaxResult(doc) {
	var type = GetNode(doc, 'type');
	var status;
	var title;
	var message;
	
	if (type == 'Message')
	{
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
	else if (type == 'Raw')
	{
		return { code: GetNode(doc, 'code'), message: GetNode(doc, 'message') };
	}
	else if (type == 'Status')
	{
	    return { code: GetNode(doc, 'code') };
	}
	else if (type == 'Array')
	{
	    return { 'array': GetNode(doc, 'array') };
	}
	else if (type == 'Dictionary') {
	    var a = new Array();
	    var xmlDoc = $.parseXML(doc);
	    var xml = $(doc);
	    var e = xml.find('array').find('item').each(function () {
	        a.push({ id: $(this).find('id').text(), value: $(this).find('value').text() });
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
	else
	{
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
	for (i = 0; i < 25; i += 5)
	{
		for (j = 1; j < 5; j++)
		{
			slug = slug.replace(aeiou.charAt(i + j), aeiou.charAt(i));
		}
	}
	slug = slug.replace(rogex, '-');

	$("#slug").val(slug);
}

function ShowSpamComment(id) {
	var commentDiv = $("#comment-" + id);
	var commentADiv = $("#comment-a-" + id);
	if (commentDiv.style.display == "block")
	{
		// hide
		commentDiv.style.display = "none";
		commentADiv.val("show comment");
	}
	else
	{
		// show
		commentDiv.style.display = "block";
		commentADiv.val("hide comment");
	}
	
	return false;
}

function FindFriendId() {
	var res = array();
	
	res[0] = 0;
	res[1] = 'Anonymous';
	
	return res;
}

function EnableDateTimePickers() {
    for(i in dtp)
    {
        $('#' + dtp[i][0]).hide();
        $('#' + dtp[i][1]).show();
    }
}

function ParseDatePicker(n) {
    var el = $('#' + n);
    
    return PostToPage(SubmitedDate, "api/functions", new Array(el), new Array(new Array("ajax", "true"), new Array("fun", "date"), new Array("date", el.val())));
}

function ParseTimePicker(n) {
	var el = $('$' + n);

	return PostToPage(SubmitedTime, "api/functions", new Array(el), new Array(new Array("ajax", "true"), new Array("fun", "time"), new Array("time", el.val())));
}

function SubmitedDate(r, e) {
	e[0].value = r['messsage'];
}

function SubmitedTime(r, e) {
	e[0].value = r['message'];
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

function namesRequested(r, e) {
    e(r);
}

$(document).ready(function () {
    if ($(".user-droplist .textbox").length > 0) {
        $(".user-droplist .textbox")
        .bind("keydown", function (event) {
            if (event.keyCode === $.ui.keyCode.TAB &&
                        $(this).data("autocomplete").menu.active) {
                event.preventDefault();
            }
        })
        .autocomplete({
            minLength: 0,
            source: function (request, response) {
                PostToPage(namesRequested, "api/friends", response, { ajax: "true", "name-field": request.term });
            },
            focus: function () {
                // prevent value inserted on focus
                return false;
            },
            select: function (event, ui) {
                this.value = "";
                if (cv($(this).siblings('.ids'), ui.item.id) == 0) {
                    $(this).before($('<span class="username">' + ui.item.value + '<span class="delete" onclick="rvl($(this).parent().siblings(\'.ids\'),' + ui.item.id + '); $(this).parent().remove();">x</span><input type="hidden" id="user-' + ui.item.id + '" name="user[' + ui.item.id + ']" value="' + ui.item.id + '" /></span>'));
                    avl($(this).siblings('.ids'), ui.item.id);
                }
                return false;
            },
            position: { collision: "flip" }
        })
        .data("autocomplete")._renderItem = function (ul, item) {
            return $('<li class="droplist-user">')
                .data("item.autocomplete", item)
                .append('<a><img src="' + item.tile + '" />' + item.value + '</a>')
                .appendTo(ul);
        };
    };
});

$(document).ready(function () {
    if ($(".permission-group-droplist .textbox").length > 0) {
        $(".permission-group-droplist .textbox").each(function () {
            var itemId = $(this).siblings('.item-id').val();
            var itemTypeId = $(this).siblings('.item-type-id').val();
            var textbox = $(this);
            var empty = $(this).parent().children(".empty");
            var border = textbox.outerWidth() - textbox.width();

            textbox.bind("keydown", function (event) {
                if (event.keyCode === $.ui.keyCode.TAB &&
                        $(this).data("autocomplete").menu.active) {
                    event.preventDefault();
                }
            })
            .bind("focus", function (event) {
                textbox.width(textbox.parent().width() - textbox.position().left - border + 'px');
                empty.hide();
            })
            .bind("click", function (event) {
                textbox.autocomplete("search", "");
                empty.hide();
            })
            .bind("blur", function (event) {
                if (textbox.siblings('.ids').val() == '') {
                    empty.show();
                }
                textbox.width('48px').width(textbox.parent().width() - textbox.position().left - border + 'px');
            })
            .autocomplete({
                minLength: 0,
                source: function (request, response) {
                    PostToPage(namesRequested, "api/acl/get-groups", response, { ajax: "true", "name-field": request.term, item: itemId, type: itemTypeId });
                },
                focus: function () {
                    // prevent value inserted on focus
                    return false;
                },
                select: function (event, ui) {
                    this.value = "";
                    if (cv($(this).siblings('.ids'), ui.item.typeId + '-' + ui.item.id) == 0) {
                        $(this).before($('<span class="' + ((ui.item.id > 0) ? 'username' : 'group') + '">' + ui.item.value + '<span class="delete" onclick="rvl($(this).parent().siblings(\'.ids\'),\'' + ui.item.typeId + '-' + ui.item.id + '\'); $(this).parent().remove();">x</span><input type="hidden" id="group-' + ui.item.typeId + '-' + ui.item.id + '" name="group[' + ui.item.TypeId + ',' + ui.item.id + ']" value="' + ui.item.typeId + ',' + ui.item.id + '" /></span>'));
                        avl($(this).siblings('.ids'), ui.item.typeId + '-' + ui.item.id);
                        empty.hide();
                        textbox.width('48px').width(textbox.parent().width() - textbox.position().left - border + 'px');
                    }
                    return false;
                },
                position: { collision: "flip" }
            })
            .data("autocomplete")._renderItem = function (ul, item) {
                return $('<li class="droplist-' + ((item.id > 0) ? 'user' : 'group') + '">')
                    .data("item.autocomplete", item)
                    .append('<a>' + ((item.tile != '') ? '<img src="' + item.tile + '" />' : '') + item.value + '</a>')
                    .appendTo(ul);
            };

            empty.bind("click", function (event) {
                textbox.autocomplete("search", "");
                empty.hide();
            });
        });
    };
});

function DeleteStatus(i) {
    return PostToAccount(DeletedStatus, "profile", "status", -1, { mode: "delete", id: i }, i);
}

function DeletedStatus(r, e, a) {
    $("#status-" + a).remove();
}

function ShowPages() {
    $(".page-tiles").dialog({
        modal: true,
        closeText: "back",
        draggable: false,
        resizable: false
    });
    return false;
}

function ShowSearch() {
    $("#search").dialog({
        modal: true,
        closeText: "back",
        draggable: false,
        resizable: false
    });
    return false;
}

function ShowMobileMenu() {
    return false;
}

function ShowPoststatus() {
    $("#post-status-div").dialog({
        modal: true,
        closeText: "back",
        draggable: false,
        resizable: false
    });
    return false;
}