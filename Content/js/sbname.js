/* ====================================================
*
*	Starbound Name Formatter for JQuery
*		Author: 	Lachee
*		Website: 	voidpixel.com.au
*
*		License: 	Creative Commons Attribution-ShareAlike 4.0 International 
*						( https://creativecommons.org/licenses/by-sa/4.0/ )
*
* ====================================================
*/

//escapeStarboundTags <- plain text
//formatStarboundTags <- html

/**
*	Converts a starbound formatted name into plain text. Removing any colour codes.
*/
function escapeStarboundTags(name) {
	
	//Make sure we have a name
	if (name == null) return "";
	
	//prepare the regex
	var re = "/\\^(.*?)(?=\\s*\\;)\\;/"; 
	
	//Prepare the formatted name
	var safe_name = name;
	
	//Find the colour codes
	var matches = name.match(/\^(.*?)(?=\s*\;)\;/g);
	
	//Make sure we actually have some matches
	if (matches == null) return safe_name;
	
	//Iterate over each match
	$.each(matches, function( i, match ) {		
		safe_name = safe_name.replace(match, "");		
	});
	
	//Remove any HTML
	safe_name = escapeHtml(safe_name);
	
	//Trim it
	safe_name = safe_name.trim();
	
	//Return it
	return safe_name;
}

/**
*	Converts a starbound formatted name into HTML. Colours, Shadows and Resets are supported and included.	
*/
function formatStarboundTags(name) {
	
	//Make sure we have a name
	if (name == null) return "";
	
	//prepare the regex
	var re = "/\\^(.*?)(?=\\s*\\;)\\;/"; 
	
	//prepare the html name
	var html_name = name;
		
	//prepare the replace close
	var replace_close = "";
		
	//Find all the colour codes
	var matches = name.match(/\^(.*?)(?=\s*\;)\;/g);
	
	//Make sure we actually have some matches
	if (matches == null) return html_name;
	
	//Iterate over each match
	$.each(matches, function( i, match ) {
		
		//Format the color
		var color = match;
		color = color.replace("^", "").replace(";", "");
		color = color.trim();
		color = escapeHtml(color);	
		
		var color_lower = color.toLowerCase();
					
		//Create the replace pattern
		var replace = "";
		
		//Make sure we end the other tags
		if (i != 0) replace = replace_close;	
		
		//Get the approparite replace format
		switch(color_lower) {
			default:			
				replace += "<span style='color: " + color + ";'>";
				replace_close = "</span>";
				break;

			case "black":
				replace += "<span id='black' style='color:black;'>";				
				replace_close = "</span>";								
				break;
				
			case "white":
				replace += "<span id='white' style='color:white;'>";				
				replace_close = "</span>";
				break;
				
			case "shadow":			
				replace = "<span style='text-shadow: 0px 1px black, 0px 2px black'>";
				replace_close = "";
				break;
				
			case "reset":			
				replace = "";	
				replace_close = "";			
				break;
		}
		
		if (color_lower == "reset") {
			
			//We are to reset, so we must close all previous tags
			//Get the position of the tag
			var reset_pos = html_name.indexOf(match);
			
			//Get the part we must close
			var open_html = html_name.substring(0, reset_pos);
			
			//Get what part we must ignore. This will get appended afterwards
			var ignore_html = html_name.substring(reset_pos + 7);
			
			//Close the open tags
			open_html = closeTags(open_html);
			
			//set the name to the html and append the ignored
			html_name = open_html + ignore_html;
		}else{
			
			//Do a simple replacement
			html_name = html_name.replace(match, replace);
			
		}
	});
	
	//Close any left over tags
	html_name = closeTags(html_name);	
	
	//Incase it in the approparite span
	html_name = "<span class='starboundtext'>" + html_name + "</span>";
	
	//Trim it
	html_name = html_name.trim();
	
	//Aaanndd, return
	return html_name;
}
		
//Source: http://stackoverflow.com/a/31485232/5010271
function closeTags(html) {
	var div = document.createElement('div');
	div.innerHTML = html
	return (div.innerHTML);		
}			

//Source: http://stackoverflow.com/a/4835406/5010271
function escapeHtml(text) {
	var map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
	return text.replace(/[&<>"']/g, function(m) { return map[m]; });
}