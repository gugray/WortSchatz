/// <reference path="../lib/jquery-2.1.4.min.js" />
/// <reference path="../lib/countUp.js" />
/// <reference path="page.js" />

var wsAdmin = (function () {
  "use strict";

  var reqId = 0;

  // Register our init script
  wsPage.registerInitScript("/admin", init);

  function init() {
    $("#btn-export").click(function () {
      // Make request
      ++reqId;
      var id = reqId;
      var data = { secret: $("#exportSecret").val() };
      // Submit request
      var req = $.ajax({
        url: "/api/export",
        type: "POST",
        contentType: "application/x-www-form-urlencoded; charset=UTF-8",
        data: data
      });
      req.done(function (data) {
        if (data == undefined || data == null) $("#export-label").text("\error/");
        else $("#export-label").text(data);
      });
      req.fail(function (jqXHR, textStatus, error) {
        $("#export-label").text("\request failed/");
      });
    });
  }

})();
