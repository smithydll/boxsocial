var newTag = new Array(0,0,0);

function CreateUserTagNearPointer(id, event)
{
	var photo = ge("photo-640");
	photox = (event.offsetX) ? event.offsetX : event.pageX-document.getElementById("photo-640").offsetLeft;
	photoy = (event.offsetY) ? event.offsetY : event.pageY-document.getElementById("photo-640").offsetTop;
	
	newTag[0] = photox;
	newTag[1] = photoy;
	
	//PostToAccount(UserTagCreated, "gallery", "tag", id, null);
	
	var tags = ge("tags").value;
	
	var nli = document.createElement('li');
	
	apc("user-tags", nli);
	
	var nlii = document.createElement('input');
	nlii.type = 'input';
	nlii.name = 'name[' + tags + ']';
	nli.appendChild(nlii);
	
	var nih = document.createElement('input');
	nih.type = 'hidden';
	nih.name = 'tag[' + tags + ']';
	nih.value = photox + ',' + photoy;
	
	apc("fieldlist", nih);

	ge("tags").value = parseInt(tags) + 1;
	
	try
	{
		hide("no-tags");
	} catch (e) {};
}

function UserTagCreated(texts)
{
}

function ShowUserTag(id)
{
	ge("tag-" + id).style.border = "solid 2px white";
}

function HideUserTag(id)
{
	ge("tag-" + id).style.border = "solid 2px transparent";
}

function ShowUserTagName(id)
{
	show("tag-name-" + id);
}

function HideUserTagName(id)
{
	hide("tag-name-" + id);
}

function ShowTagNearPointer(event)
{
	var photo = ge("photo-640");
	photox = (event.offsetX) ? event.offsetX : event.pageX-document.getElementById("photo-640").offsetLeft;
	photoy = (event.offsetY) ? event.offsetY : event.pageY-document.getElementById("photo-640").offsetTop;
	
	var closestId = -1;
	var closestDist = Math.pow(10000, 2);
	var i;
	for (i in user_tags)
	{
		var dist = Math.abs(Math.pow(user_tags[i][1] + 50, 2) - Math.pow(photox, 2)) + Math.abs(Math.pow(user_tags[i][2] + 50, 2) - Math.pow(photoy, 2));
		if (dist < closestDist && dist <= Math.pow(200, 2))
		{
			closestId = user_tags[i][0];
			closestDist = dist;
		}
	}
	
	for (i in user_tags)
	{
		if (closestId == user_tags[i][0])
		{
			ShowUserTagName(user_tags[i][0]);
		}
		else
		{
			HideUserTagName(user_tags[i][0]);
		}
	}
}
