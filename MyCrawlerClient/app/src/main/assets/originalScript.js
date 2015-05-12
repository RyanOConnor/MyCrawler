var x = document.getElementsByTagName('a');
for(var i = 0; i < x.length; i++){
    x[i].addEventListener("click", highlight, false);
    x[i].addEventListener("click", locateNode, false);
}

function highlight() {
  this.style.backgroundColor = "yellow";
  this.style.color = "black";
  this.href = "javascript:void(0);";
  if(this.className.indexOf(" ") != -1){
      var classes = this.className.split(' ');
      var query = classes[0];
      for(var j = 1; j < classes.length; j++) {
          query = query + " " + classes[j];
      }
      var nodes = document.getElementsByClassName(query);
      for(var k = 0; k < nodes.length; k++){
          nodes[k].style.backgroundColor = "yellow";
          nodes[k].style.color = "black";
          nodes[k].href = "javascript:void(0);";
      }
  } else {
      var nodes = document.getElementsByClassName(this.className);
      for(var k = 0; k < nodes.length; k++) {
          nodes[k].style.backgroundColor = "yellow";
          nodes[k].style.color = "black";
          nodes[k].href = "javascript:void(0);";
      }
  }
}

function locateNode() {
  if(!this.className) {
      var retValue = constructNodePath(this);
      window.HTMLOUT.showHTML(retValue);
  } else {
      window.HTMLOUT.showHTML(this.className);
  }
}

function constructNodePath(node) {
  var nodePath = node.tagName + node.className;
  while(!node.className) {
      node = node.parentNode;
      if(node.className){
          nodePath = node.tagName + '.' + node.className + ' ' + nodePath;
      } else {
          nodePath = node.tagName + ' ' + nodePath;
      }
  }
}