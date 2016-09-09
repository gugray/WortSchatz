/// <reference path="../lib/jquery-2.1.4.min.js" />
/// <reference path="page.js" />

var wsResult = (function () {
  "use strict";

  var reqId = 0;

  // Register our init script
  wsPage.registerInitScript("/ergebnis", init);

  // Serves as "onload", invoked by single-page controller when page loads.
  function init() {
    // UID to request: from URL
    var regex = /\/ergebnis\/(.+)$/;
    var match = regex.exec(wsPage.getRel());
    // Make request
    ++reqId;
    var id = reqId;
    var data = { uid: match[1] };
    // Submit request
    var req = $.ajax({
      url: "/api/getscore",
      type: "POST",
      contentType: "application/x-www-form-urlencoded; charset=UTF-8",
      data: data
    });
    req.done(function (data) {
      if (id != reqId) return;
      $("#result-value").text(data);
    });
  }
})();
