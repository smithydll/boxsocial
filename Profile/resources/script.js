function toggleStatusComments(parent, id, type, el) {
    if (parent.hasClass('active')) {
        parent.removeClass('active');
    } else {
        LoadComments(id, type, el);
        parent.addClass('active');
    }
    return false;
}