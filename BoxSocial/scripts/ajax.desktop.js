// viewgroupgallery.html
function StarOver(stars, itemId, itemType) {
    var i = 1;
    for (i = 1; i <= stars; i++) {
        $("#rate-" + i + "s-" + itemId + "-" + itemType).attr("src", "/images/star-on.png");
    }
    for (i = stars + 1; i <= 5; i++) {
        var itemName = "#rate-" + i + "s-" + itemId + "-" + itemType;
        if ($(itemName).hasClass("rank-on")) {
            $(itemName).attr("src", "/images/star-on.png");
        }
        else {
            $(itemName).attr("src", "/images/star-off.png");
        }
    }
}

// viewgroupgallery.html
function SubmitRating(stars, itemId, itemType) {
    return PostToPage(null, "api/rate?ajax=true&rating=" + stars + "&item=" + itemId + "&type=" + itemType, null, null, null);
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
        .bind("focus", function (event) {
            $(this).parent().css("outline", "#ff9955 auto 5px");
        })
        .bind("blur", function (event) {
            $(this).parent().css("outline", "none");
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
                if ($(this).parent().hasClass('multiple') || $(this).siblings('.ids').val() == '') {
                    if (cv($(this).siblings('.ids'), ui.item.id) == 0) {
                        $(this).before($('<span class="username">' + ui.item.value + '<span class="delete" onclick="rvl($(this).parent().siblings(\'.ids\'),' + ui.item.id + '); $(this).parent().siblings(\'.ids\').trigger(\'change\'); $(this).parent().remove();">x</span><input type="hidden" id="user-' + ui.item.id + '" name="user[' + ui.item.id + ']" value="' + ui.item.id + '" /></span>'));
                        avl($(this).siblings('.ids'), ui.item.id);
                        $(this).siblings('.ids').trigger("change");
                    }
                }
                return false;
            },
            position: { collision: "flip" }
        })
        .data("ui-autocomplete")._renderItem = function (ul, item) {
            return $('<li class="droplist-user">')
                .data("item.autocomplete", item)
                .append('<a><img src="' + item.tile + '" />' + item.value + '</a>')
                .appendTo(ul);
        };
    };
});

$(document).ready(function () {
    preparePermissionsList('');
});

function preparePermissionsList(id) {
    if ($(id + ".permission-group-droplist .textbox").length > 0) {
        $(id + ".permission-group-droplist .textbox").each(function () {
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
                textbox.parent().css("outline", "#ff9955 auto 5px");
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
                textbox.parent().css("outline", "none");
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
                        $(this).before($('<span class="' + ((ui.item.id > 0) ? 'username' : 'group') + '">' + ui.item.value + '<span class="delete" onclick="rvl($(this).parent().siblings(\'.ids\'),\'' + ui.item.typeId + '-' + ui.item.id + '\'); $(this).parent().siblings(\'.ids\').trigger(\'change\'); $(this).parent().remove();">x</span><input type="hidden" id="group-' + ui.item.typeId + '-' + ui.item.id + '" name="group[' + ui.item.TypeId + ',' + ui.item.id + ']" value="' + ui.item.typeId + ',' + ui.item.id + '" /></span>'));
                        avl($(this).siblings('.ids'), ui.item.typeId + '-' + ui.item.id);
                        empty.hide();
                        textbox.width('48px').width(textbox.parent().width() - textbox.position().left - border + 'px');
                        $(this).siblings('.ids').trigger("change");
                    }
                    return false;
                },
                position: { collision: "flip" }
            })
            .data("ui-autocomplete")._renderItem = function (ul, item) {
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
}

function ShareItem(itemId, itemType) {
    return PostToPage(ShowShareContent, "api/share?ajax=true&mode=post&item=" + itemId + "&type=" + itemType, null, { ajax: "true" }, { item: itemId, type: itemType });
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

/* http://stackoverflow.com/questions/13442897/jquery-animate-backgroundposition-not-working */
$.cssHooks["backgroundPositionY"] = {
    get: function (elem, computed, extra) {
        return elem.style.backgroundPosition.split(' ')[1];
    },
    set: function (elem, value) {
        var x = elem.style.backgroundPosition.split(' ')[0];
        elem.style.backgroundPosition = x + ' ' + value;
    }
};

$(document).ready(function () {
    $('.info').on('mouseenter', function () {
        if ($(this).height() == 100 && $(this).hasClass('info') && (!$(this).hasClass('overlay'))) {
            $(this).clone().addClass('overlay').css('position', 'absolute').css('width', '775px').css('z-index', '10').prependTo($(this).parent()).animate({ height: '190px', 'backgroundPositionY': '0px' }, 500);

            $('.info.overlay').on('mouseleave', function () {
                $(this).delay('350').animate({ height: '100px', 'backgroundPositionY': '-50px' }, 500, function () {
                    $(this).remove();
                });
            });

            $('.info.overlay').on('mouseenter', function () {
                $(this).clearQueue();
            });
        }
    });
});

$(document).ready(function () {
    $(".username-card").on('mouseenter', function (e) {
        if (!$(this).parent().hasClass('contact-card-container')) {
            PostToPage(LoadedCard, "api/card", $(this), { ajax: 'true', uid: $(this).attr('bs-uid') }, e.pageX - 5);
        }
    });
});

function LoadedCard(r, e, a) {
    $('.contact-card').remove();
    if (a + 370 > $(document).width()) {
        a = $(document).width() - 370;
    }
    a = a - (e.offset().left - e.position().left);
    e.wrap('<span class="contact-card-container"></span>');
    var loc = (r['location'] != 'FALSE') ? '<p><strong>' + r['l-location'] + ':</strong> ' + r['location'] + '</p>' : '';
    e.after('<div class="contact-card" style="left: ' + a + 'px;"><div style="height: 80px; background-image: url(\'' + r['cover-photo'] + '\'); background-position: center; background-color: #333333; background-size: cover;"><img src="' + r['display-picture'] + '" /></div><div><p><strong><a href="' + r['uri'] + '">' + r['display-name'] + '</a></strong></p><p><span class="subscribe-' + r['type'] + '-' + r['id'] + ' subscribe-button' + (r['subscribed'] == 'true' ? "subscribed" : "") + '"><a href="' + r['subscribe-uri'] + '" onclick="return SubscribeItem(' + r['id'] + ',' + r['type'] + ', $(this).parent().hasClass("subscribed") );">' + r['l-subscribe'] + '</a></span><span class="subscribers">' + r['subscribers'] + '</span></p>' + loc + '<p>' + r['abstract'] + '</p></div></div>');
    $(".contact-card-container").on('mouseleave', function (e) {
        $('.contact-card').unwrap().remove();
    });
}

function CheckRelationship(a, b) {
    switch ($('#' + a).val()) {
        case "2":
        case "3":
        case "4":
        case "5":
            $('#' + b).show();
            break;
        default:
            $('#' + b).hide();
            break;
    }
}
