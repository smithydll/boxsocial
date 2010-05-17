var host = "/";
var dtp = Array(); /* Date Time Pickers */
var usb = Array(); /* User Select Boxes */

function htmlEncode(i) {
	i = i.replace("&","&amp;");
	i = i.replace("<","&lt;");
	i = i.replace(">","&gt;");
	i = i.replace("\"","&quot;");
	return i;
}

function ge(n)
{
	return document.getElementById(n);
}

function hide(n)
{
	ge(n).style.display = 'none';
}

function show(n)
{
	ge(n).style.display = 'block';
}

function sv(e, v)
{
	e.innerHTML = v;
}

function setv(n, v)
{
	sv(ge(n), v);
}

function ac(e, c)
{
	e.appendChild(c);
}

function apc(n, c)
{
	ac(ge(n), c);
}

function ce(el)
{
	return document.createElement(el);
}

function StarOver(stars, itemId, itemType)
{
	var i = 1;
	for (i = 1; i <= stars; i++)
	{
		ge("rate-" + i + "s-" + itemId + "-" + itemType).setAttribute("src", "/images/star-on.png");
	}
	for (i = stars + 1; i <= 5; i++)
	{
		itemName = "rate-" + i + "s-" + itemId + "-" + itemType;
		if (ge(itemName).className == "rank-on")
		{
			ge(itemName).setAttribute("src", "/images/star-on.png");
		}
		else
		{
			ge(itemName).setAttribute("src", "/images/star-off.png");
		}
	}
}

function SubmitRating(stars, itemId, itemType)
{
	return PostToPage(null, "rate.aspx?ajax=true&rating=" + stars + "&item=" + itemId + "&type=" + itemType, null, null);
}

function SendStatus()
{
	var statusMessage = ge('message').value;
	return PostToAccount(SentStatus, "profile", "status", -1, new Array(new Array('message', statusMessage)));
}

function SentStatus(r, e)
{
	hide('status-form');
	show('status-message');
	setv('status-message', r[1]);
}

function DeleteComment(id, iid)
{
	return PostToPage(null, "comment/?mode=delete&item=" + id, null, new Array(new Array("ajax", "true")));
}

function ReportComment(id, iid)
{
	return PostToPage(null, "comment/?mode=report&ajax=true&item=" + id, null, new Array(new Array("ajax", "true")));
}

var ciid;
function QuoteComment(id, iid)
{
	ciid = iid;
	return PostToPage(QuotedComment, "comment/?ajax=true&mode=fetch&item=" + id, null, new Array(new Array("ajax", "true")));
}

function QuotedComment(r, e)
{
	setv("comment-text-" + ciid, r[1]);
	ge("comment-text-" + ciid).focus();
}

var csort;
var cid;
function SubmitComment(id, type, zero, sort)
{
	csort = sort;
	cid = id;
	return PostToPage(SubmitedComment, "comment/", null, new Array(new Array("ajax", "true"), new Array("item", id), Array("type", type), new Array("comment", escape(ge("comment-text-" + id).value).replace('+','%2B'))));
}

function SubmitedComment(r, e)
{
	var nli = ce('div');
	sv(nli, r[1]);
	if (csort == 'desc')
	{
		var n = ge("comment-list");
		if (n.hasChildNodes())
		{
			n.insertBefore(nli, n.firstChild);
		}
		else
		{
			apc("comment-list", nli);
		}
	}
	else
	{
		apc("comment-list", nli);
	}
	hide("comment-form");

	var submitBtn = ge("comment-submit-" + cid);
	submitBtn.setAttribute("disabled", "disabled");
	ge("comment-text-" + cid).setAttribute("disabled", "disabled");
	submitBtn.setAttribute("value", "Comment Posted.");

	try
	{
		hide("no-comments");
	} catch (e) {};
}

function SubmitListItem(id)
{
	var text = ge("text").value;
	return PostToAccount(SubmitedListItem, "pages", "lists", id, new Array(new Array("mode", "append"), new Array("save", "Add"), new Array("text", escape(text).replace('+','%2B'))));
}

function SubmitedListItem(r, e)
{
	var nli = ce('li');
	sv(nli, r[1]);
	apc("list-list", nli);

	try
	{
		hide("no-list-items");
	} catch (e) {};
}

function AppendRandSid(uri)
{
	var cacheRnd=parseInt(Math.random()*99999999); // cache busters
	if (uri.indexOf("?") >= 0)
	{
		uri = uri + "&";
	}
	else
	{
		uri = uri + "?";
	}
	return uri + "rnd=" + cacheRnd + "&sid=" + sid;
}

function PostToAccount(onPost, module, sub, id, params)
{
	var par = new Array(new Array("module", module), new Array("sub", sub), new Array("id", id));
	if (params != null)
	{
		return PostToPage(onPost, "account/?ajax=true", null, par.concat(params));
	}
	else
	{
		return PostToPage(onPost, "account/?ajax=true", null, par);
	}
}

function PostToPage(onPost, page, nodes, params)
{
	var par = '';
	var i = 0;
	if (params != null)
	{
		for(i in params)
		{
			par = par + "&" + params[i][0] + "=" + params[i][1];
		}
	}
		
	var xhr_lang = createXHR();
	if (par != '')
	{
		xhr_lang.open("POST", host + AppendRandSid(page), true);
		xhr_lang.setRequestHeader("Content-type", "application/x-www-form-urlencoded");
		xhr_lang.setRequestHeader("Content-length", par.length);
		xhr_lang.setRequestHeader("Connection", "close");
	}
	else
	{
		xhr_lang.open("GET", host + AppendRandSid(page), true);
	}
	
	xhr_lang.onreadystatechange = function()
	{ 
		if(xhr_lang.readyState == 4)
		{
			if(xhr_lang.status == 200)
			{ 
				if (onPost != null)
				{
					var r = ProcessAjaxResult(xhr_lang);
					if (r != null) onPost(r, nodes);
				}
				else
				{
					ProcessAjaxResult(xhr_lang);
				}
			}	
		} 
	}; 
	
	xhr_lang.send(par);
	
	return false;
}

function ProcessAjaxResult(xhr_lang)
{
	var doc;
	if (navigator.userAgent.indexOf("Firefox") >= 0)
	{
		doc = (new DOMParser()).parseFromString(xhr_lang.responseText, 'text/xml');
	}
	else
	{
		doc = xhr_lang.responseXML;
	}
				
	var type = GetNode(doc, 'type');
	var status;
	var title;
	var message;
	
	if (type == 'Message')
	{
		DisplayMessage(GetNode(doc, 'title'), GetNode(doc, 'message'));
		return null;
	}
	else if (type == 'Raw')
	{
		return new Array(GetNode(doc, 'code'), GetNode(doc, 'message'));
	}
	else if (type == 'Status')
	{
		return new Array(GetNode(doc, 'code'));
	}
	else if (type == 'Array')
	{
		return new Array(GetNode(doc, 'array'));
	}
	else if (type == 'Dictionary')
	{
		var a = Array();
		var n = doc.getElementsByTagName('array')[0].children;
		for (i = 0; i < n.length; i++)
		{
			var o = n[i].children;
			a.push(new Array(o[0].textContent, o[1].textContent));
		}
		return a;
	}
	else
	{
		return xhr_lang.responseText;
	}
}

function GetNode(doc, node)
{
	var n = doc.getElementsByTagName(node)[0];
	return n.text || n.textContent;
}

function DisplayMessage(title, message)
{
	window.alert(message);
}

function createXHR()
{
	try {
		netscape.security.PrivilegeManager.enablePrivilege("UniversalBrowserRead");
	} catch (e) { }

	var req = null;
	var cases = ["Msxml2.XMLHTTP","MSXML2.XMLHTTP.5.0","MSXML2.XMLHTTP.4.0","MSXML2.XMLHTTP.3.0","MICROSOFT.XMLHTTP.1.0","MICROSOFT.XMLHTTP.1","MICROSOFT.XMLHTTP"];
	var i = 0;
	for(i in cases)
	{
		var casei=cases[i];
		try
		{
			req = new ActiveXObject(casei);
		}
		catch(o)
		{
			req = null;
		}
		if(req)
		{
			break;
		}
	}
	if (req == null && window.XMLHttpRequest)
	{
		req = new XMLHttpRequest;
	}
	return req;
}

function UpdateSlug()
{
	var aeiou = "aáäàâeéëèêiíïìîoóöòôuúüùû";
	var rogex = /([\W]+)/g;
	var titleBox = ge("title");
	var slugBox = ge("slug");
	
	var slug = titleBox.value.toLowerCase();
	
	var i, j;
	for (i = 0; i < 25; i += 5)
	{
		for (j = 1; j < 5; j++)
		{
			slug = slug.replace(aeiou.charAt(i + j), aeiou.charAt(i));
		}
	}
	slug = slug.replace(rogex, '-');
	
	slugBox.value = slug;
}

function ShowSpamComment(id)
{
	var commentDiv = ge("comment-" + id);
	var commentADiv = ge("comment-a-" + id);
	if (commentDiv.style.display == "block")
	{
		// hide
		commentDiv.style.display = "none";
		sv(commentADiv, "show comment");
	}
	else
	{
		// show
		commentDiv.style.display = "block";
		sv(commentADiv, "hide comment");
	}
	
	return false;
}

function FindFriendId()
{
	var res = array();
	
	res[0] = 0;
	res[1] = 'Anonymous';
	
	return res;
}

function EnableDateTimePickers()
{
    for(i in dtp)
    {
        hide(dtp[i][0]);
        show(dtp[i][1]);
    }
}

function ParseDatePicker(n)
{
    var el = ge(n);
    var val = el.value;
    
    return PostToPage(SubmitedDate, "functions", new Array(el), new Array(new Array("ajax", "true"), new Array("fun", "date"), new Array("date", val)));
}

function ParseTimePicker(n)
{
	var el = ge(n);
    var val = el.value;

	return PostToPage(SubmitedTime, "functions", new Array(el), new Array(new Array("ajax", "true"), new Array("fun", "time"), new Array("time", val)));
}

function SubmitedDate(r, e)
{
	e[0].value = r[1];
}

function SubmitedTime(r, e)
{
	e[0].value = r[1];
}

function PickUserName(n)
{
	var el = ge(n);
	var val = el.value;

	return PostToPage(SubmittedUserSelectBox, "functions", new Array(el), new Array(new Array("ajax", "true"), new Array("fun", "user-list"), new Array("name-field", val)));
}

function SubmittedUserSelectBox(r, e)
{
	var s = e[0].id.substring(0, e[0].id.length - 5);
	var n = s + '[dropbox]';
	show(n);

	setv(n, '');
	for (i in r)
	{
		var nli = ce('li');
		var nli2 = ce('a');
		sv(nli2, r[i][1]);
		nli2.onclick = "SelectName('" + s + "', new Array('" + r[i][0] + "','" + r[i][1] + "'))";
		nli2.href = '#';
		ac(nli, nli2);
		apc(n, nli);
	}
}

function SelectName(n, i)
{
	var e = ge(n + '[raw]');
	e.value = '';
	var c = ce('span');
	var r = ce('a');
	sv(r, 'X');
	c.class = 'username-name';
	sv(c, i[1] + '&nbsp;');
	ac(c, r);
	apc(n + '[list]', c);
}

function RemoveName(n, i)
{
}
