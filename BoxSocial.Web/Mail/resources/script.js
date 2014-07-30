
var sendingMessage = false;
function SendMessage(id, text) {
    if (text != '') {
        sendingMessage = true;
        PostToPage(SubmitedMessage, "account", null, { ajax: "true", id: id, module: "mail", sub: "compose", mode: "reply", message: text, 'newest-id': nid, save: 'true' });
    }
    return false;
}

function SubmitedMessage(r, e) {
    $('#posts').append(r['template']);
    $('#posts').scrollTop($('#posts').prop("scrollHeight"));
    $('.comment-textarea').val('');
    if (r['newest-id'] > 0) {
        nid = r['newest-id'];
    }
    sendingMessage = false;
}

function checkNewMessagesInThread() {
    if (document.hidden || document.webkitHidden || document.msHidden) {
        return;
    }
    loadNewMessagesInThread(queryMode);
}

function loadNewMessagesInThread(mode) {
    if (sendingMessage) return false;
    PostToAccount(LoadedNewMessages, "mail", "message", tid, { mode: 'poll', 'newest-id': nid });
    return false;
}

function LoadedNewMessages(r, e, a, c) {
    if (sendingMessage) return;
    if (c == 'noNewContent') {
    }
    else if (c == 'newMessages') {
        if (r['update'] == 'true') {
            $('#posts').append(r['template']);
            $('#posts').scrollTop($('#posts').prop("scrollHeight"));
        }
        if (r['newest-id'] > 0) {
            nid = r['newest-id'];
        }
    }
}
