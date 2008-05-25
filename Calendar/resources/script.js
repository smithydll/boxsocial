function MarkTaskComplete(id)
{
	var texts = PostToAccount(null, "calendar", "task-complete", id, null).responseText;
	
	return false;
}
