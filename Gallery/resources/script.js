var newTag = new Array(0,0,0);

function CreateUserTagNearPointer(id, event)
{
	var photo = $("#photo-640");
	photox = (event.offsetX) ? event.offsetX : event.pageX-document.getElementById("photo-640").offsetLeft;
	photoy = (event.offsetY) ? event.offsetY : event.pageY-document.getElementById("photo-640").offsetTop;
	
	newTag[0] = photox;
	newTag[1] = photoy;
	
	//PostToAccount(UserTagCreated, "gallery", "tag", id, null);
	
	var tags = $("#tags").val();
	
	var nli = $('<li>');
	
	$("#user-tags").append(nli);
	
	var nlii = $('<input>');
	nlii.type = 'input';
	nlii.name = 'name[' + tags + ']';
	nli.append(nlii);
	
	var nih = $('<input>');
	nih.type = 'hidden';
	nih.name = 'tag[' + tags + ']';
	nih.value = photox + ',' + photoy;
	
	$("#fieldlist").append(nih);

	$("#tags").val(parseInt(tags) + 1);
	
	$("#no-tags").hide();
}

function UserTagCreated(texts)
{
}

function ShowUserTag(id)
{
	$("#tag-" + id).style.border = "solid 2px white";
}

function HideUserTag(id)
{
	$("#tag-" + id).style.border = "solid 2px transparent";
}

function ShowUserTagName(id)
{
	$("#tag-name-" + id).show();
}

function HideUserTagName(id)
{
	$("#tag-name-" + id).hide();
}

function ShowTagNearPointer(event)
{
	var photo = $("#photo-640");
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

function toggleHd() {
    if (parent.location.hash != '#hd') {
        showHd();
    } else {
        showNormal();
    }
}

function showHd() {
    document.body.style.overflow = 'hidden';

    if ($("#display-hd").height() > 1280) {
        $('#photo-hd').attr('src', hdDisplay);
    }
    else {
        $('#photo-hd').attr('src', retinaDisplay);
    }
    $('#display-hd').show();
    //parent.location.hash = 'hd';

    var Title = $(this).text();
    var History = window.History;

    if (!History.enabled) {
        parent.location.hash = 'hd';
        return false;
    }
    else {
        History.replaceState({ data: 'hd' }, Title + " • HD", "#hd");
    }

    positionHd();
}

function showNormal() {
    document.body.style.overflow = 'auto';
    $('#display-hd').hide();
    //parent.location.hash = '';

    var Title = $(this).text();
    var History = window.History;
    if (!History.enabled) {
        parent.location.hash = '#normal';
        return false;
    }
    History.replaceState({ data: 'normal' }, Title.replace(" • HD", ""), "#normal");
}
