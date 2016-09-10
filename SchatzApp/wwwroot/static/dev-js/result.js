/// <reference path="../lib/jquery-2.1.4.min.js" />
/// <reference path="../lib/countUp.js" />
/// <reference path="page.js" />

var wsResult = (function () {
  "use strict";

  var reqId = 0;

  // Register our init script
  wsPage.registerInitScript("/ergebnis", init);

  // Serves as "onload", invoked by single-page controller when page loads.
  function init() {
    // Is this a share link? Then redirect to starting page.
    var reShare = /\/ergebnis\/.{10}\/share/;
    if (reShare.test(wsPage.getRel())) {
      wsPage.navigateTo("/");
      return;
    }
    // UID to request: from URL
    var reID = /\/ergebnis\/(.{10})$/;
    var mID = reID.exec(wsPage.getRel());
    // Make request
    ++reqId;
    var id = reqId;
    var data = { uid: mID[1] };
    // Submit request
    var req = $.ajax({
      url: "/api/getscore",
      type: "POST",
      contentType: "application/x-www-form-urlencoded; charset=UTF-8",
      data: data
    });
    req.done(function (data) {
      if (id != reqId) return;
      // Show value with count up animation
      if (data < 9000) roundPrec = 100;
      else roundPrec = 500;
      var counter = new CountUp("result-value", 0, data, 0, 1.5, animOptions);
      counter.start();
      // Show rest of the page; set up links
      $("#result-link").attr("href", window.location.href);
      $("#result-link").text(window.location.href);
      $("#result-details").addClass("visible");
      var fbShareHref = "https://facebook.com/sharer/sharer.php?u=http://wortschatz.tk/ergebnis/" + mID[1] + "/share";
      $("#fb-share-link").attr("href", fbShareHref);
      // Update title
      $(document).prop("title", "Meine deutsche Wortschatzgröße ist ungefähr " + data);
    });
  }

  // Increments ("precision") during show-number animation
  var roundPrec = 100;

  // Cubic easing, with rounding to specified precision.
  var easingFn = function (t, b, c, d) {
    var ts = (t /= d) * t;
    var tc = ts * t;
    var res = b + c * (tc + -3 * ts + 3 * t);
    return Math.round(res / roundPrec) * roundPrec;
  }

  // Animation options for CountUp.
  var animOptions = {
    useEasing: true,
    easingFn: easingFn,
    useGrouping: false,
    separator: '',
    decimal: '.',
    prefix: '',
    suffix: ''
  };
})();
