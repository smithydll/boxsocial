
function SendMessage(id, text) {
    return PostToPage(SubmitedMessage, "account", null, { ajax: "true", id: id, module: "mail", sub: "compose", mode: "reply", message: text, save: 'true' });
}

function SubmitedMessage(r, e) {
    $('#posts').append(r['template']);
    $('#posts').scrollTop($('#posts').prop("scrollHeight"));
    $('.comment-textarea').val('');
}
