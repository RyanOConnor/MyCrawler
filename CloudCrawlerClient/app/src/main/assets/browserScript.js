
function start(){
    var div = document.createElement('div');
    document.body.appendChild(div);
    div.className = "waitBlock";
    div.style.backgroundColor = "black";
    div.style.position = "fixed";
    div.style.width = "100%";
    div.style.height = "100%";
    div.setAttribute("z-index", "999999");
    div.style.opacity = 0.5;
    div.style.left = 0;
    div.style.top = 0;

    var x = document.getElementsByTagName('a');
    for(var i = 0; i < x.length; i++){
    x[i].addEventListener("click", highlight, false);
    x[i].onclick = String(x[i].href);
    x[i].href = "javascript:void(0);" + x[i].href;
    }

    div = document.getElementsByClassName("waitBlock")[0];
    var parent = div.parentNode;
    parent.removeChild(div);
 }

 function highlight() {
    debugger;
    var HREF_PREFIX_LENGTH = 19;
    var prevBackground = this.style.backgroundColor;
    var prevColor = this.style.color;

    this.style.backgroundColor = "yellow";
    this.style.color = "black";

    var nodePath = getNodePath(this);
    var hrefs = [];
    var nodes = document.querySelectorAll(nodePath);
    for(var i = 0; i < nodes.length; i++){
        nodes[i].style.backgroundColor = "yellow";
        nodes[i].style.color = "black";
        var linkVal = [ nodes[i].href.substring(HREF_PREFIX_LENGTH), nodes[i].innerText ];
        hrefs.push(linkVal);
    }

    var dialog = document.createElement('dialog');
    var message = document.createElement('p');
    var yesBtn = document.createElement('button');
    var noBtn = document.createElement('button');
    document.body.appendChild(dialog);
    dialog.appendChild(message);
    dialog.appendChild(yesBtn);
    dialog.appendChild(noBtn);
    dialog.id = "confirmationDialog";
    dialog.style.height = "auto";
    dialog.style.width = "auto";
    message.innerText = "Is this correct?";
    message.style.fontSize = "medium";
    yesBtn.innerText = "Yes";
    yesBtn.style.float = "left";
    yesBtn.style.marginLeft = "10px";
    yesBtn.style.marginRight = "5px";
    noBtn.innerText = "No";
    noBtn.style.float = "left";
    yesBtn.addEventListener("click", function(){ returnToAndroid(nodePath, hrefs); }, false);
    noBtn.addEventListener("click", function(){ resetState(prevBackground, prevColor, nodes); }, false);
    document.getElementById("confirmationDialog").showModal();
    dialog.style.width = String((yesBtn.offsetWidth + noBtn.offsetWidth) * 2) + "px";
    document.getElementById("confirmationDialog").showModal();
 }

 function getNodePath(node){
    var nodePath = node.localName;
    for(var i = 0; i < node.classList.length; i++) {
        nodePath = nodePath + '.' + node.classList[i];
    }

    while( ( !node.className ) && ( node.parentNode.localName != null ) ) {
         node = node.parentNode;
         if(node.className) {
             var fullName = '';
             if(node.className.indexOf(' ') != -1){
                 var classes = node.className.split(' ');
                 for(var i = 0; i < classes.length; i++){
                     fullName = fullName + '.' + classes[i];
                 }
             } else {
                 fullName = '.' + node.className;
             }
             nodePath = node.localName + fullName + ' ' + nodePath;
         } else {
             nodePath = node.localName + ' ' + nodePath;
         }
     }

    return nodePath;
 }

 function returnToAndroid(path, links){
    window.HTMLOUT.showHTML(path, document.documentURI, JSON.stringify(links));
 }

 function resetState(prevBackground, prevColor, nodes){
    document.getElementById("confirmationDialog").open = false;
    for(var i = 0; i < nodes.length; i++){
        nodes[i].style.backgroundColor = prevBackground;
        nodes[i].style.color = prevColor;
    }
    document.getElementById("confirmationDialog").close();
 }

 start();