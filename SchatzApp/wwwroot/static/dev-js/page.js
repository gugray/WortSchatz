/// <reference path="x-jquery-2.1.4.min.js" />
/// <reference path="x-history.min.js" />
/// <reference path="x-jquery.tooltipster.min.js" />

function startsWith(str, prefix) {
  if (str.length < prefix.length)
    return false;
  for (var i = prefix.length - 1; (i >= 0) && (str[i] === prefix[i]) ; --i)
    continue;
  return i < 0;
}

function escapeHTML(s) {
  return s.replace(/&/g, '&amp;')
          .replace(/"/g, '&quot;')
          .replace(/</g, '&lt;')
          .replace(/>/g, '&gt;');
}

var kgPage = (function () {
  "use strict";

  var reqId = 0; // Current page load request ID. If page has moved on, earlier requests ignored when they complete.
  var location = null; // Full location, as seen in navbar
  var path = null; // Path after domain name
  var rel = null; // Relative path (path without language ID at start)

  // Page init scripts for each page (identified by relPath).
  var initScripts = {};
  // Global init scripts invoked on documentReady.
  var globalInitScripts = [];

  // Parse full path, language, and relative path from URL
  function parseLocation() {
    location = window.history.location || window.location;
    var rePath = /https?:\/\/[^\/]+(.*)/i;
    var match = rePath.exec(location);
    path = match[1];
    rel = path;
  }

  // Page just loaded: time to get dynamic part asynchronously, wherever we just landed
  $(document).ready(function () {
    // Make sense of location
    parseLocation();
    // Update menu to show where I am (will soon end up being)
    updateMenuState();
    // Global script initializers
    for (var i = 0; i != globalInitScripts.length; ++i) globalInitScripts[i]();
    // Request dynamic page - async
    // Skipped if we received page with content present already
    var hasContent = $("body").hasClass("has-initial-content");
    if (!hasContent) {
      ++reqId;
      var id = reqId;
      var data = { rel: rel };
      // Submit request
      var req = $.ajax({
        url: "/api/getpage",
        type: "POST",
        contentType: "application/x-www-form-urlencoded; charset=UTF-8",
        data: data
      });
      req.done(function (data) {
        dynReady(data, id);
      });
      req.fail(function (jqXHR, textStatus, error) {
        applyFailHtml();
      });
    }
    // If page has initial content, trigger dyn-content-loaded activities right now
    if (hasContent) dynReady(null, -1);
  });

  // Navigate within single-page app (invoked from link click handler)
  function dynNavigate() {
    // Make sense of location
    parseLocation();
    // Clear whatever's currently shown
    //$("#dynPage").html("");
    $("#dynPage").addClass("fading");
    // Update menu to show where I am (will soon end up being)
    updateMenuState();
    // Request dynamic page - async
    ++reqId;
    var id = reqId;
    var data = { rel: rel };
    // Submit request
    var req = $.ajax({
      url: "/api/getpage",
      type: "POST",
      contentType: "application/x-www-form-urlencoded; charset=UTF-8",
      data: data
    });
    req.done(function (data) {
      navReady(data, id);
    });
    req.fail(function (jqXHR, textStatus, error) {
      applyFailHtml();
    });
  }

  // Show error content in dynamic area
  function applyFailHtml() {
    $("#dynPage").html("Ouch.");
    // TO-DO: fail title; keywords; description
  }

  // Apply dynamic content: HTML body, title, description, keywords; possible other data
  function applyDynContent(data) {
    $(document).attr("title", data.title);
    $("#dynPage").html(data.html);
    $("#dynPage").removeClass("fading");
    // Run this page's script initializer, if any
    for (var key in initScripts) {
      if (key == "/") {
        if (rel == "/") initScripts[key](data);
      }
      else if (startsWith(rel, key)) initScripts[key](data);
    }
    // Scroll to top
    $(window).scrollTop(0);
  }

  function navReady(data, id) {
    // An obsolete request completing too late?
    if (id != reqId) return;

    // Show dynamic content, title etc.
    applyDynContent(data);
  }

  // Dynamic data received after initial page load (not within single-page navigation)
  function dynReady(data, id) {
    // An obsolete request completing too late?
    if (id != -1 && id != reqId) return;

    // Show dynamic content, title etc.
    // Data is null if we're call directly from page load (content already present)
    if (data != null) applyDynContent(data);

    // Set up single-page navigation
    $(document).on('click', 'a.ajax', function () {
      // Navigate
      history.pushState(null, null, this.href);
      dynNavigate();
      return false;
    });
    $(window).on('popstate', function (e) {
      dynNavigate();
    });

    // *NOW* that we're all done, show page.
    //$("#thePage").css("visibility", "visible");
  }

  // Updates top navigation menu to reflect where we are
  function updateMenuState() {
    $("#main-menu a").removeClass("on");
    $(".sm-item").removeClass("on");
    $(".sm-item a").removeClass("on");
    if (rel == "/" || startsWith(rel, "/customer/")) {
      $("#mm-customers").addClass("on");
      if (rel == "/") $("#sm-customers").addClass("on");
      else if (startsWith(rel, "/customer/")) $("#sm-onecustomer").addClass("on");
    }
    else if (startsWith(rel, "/reports")) {
      $("#mm-reports").addClass("on");
      $("#sm-reports").addClass("on");
      if (startsWith(rel, "/reports/all")) $("#sm-reports-all").addClass("on");
      else if (startsWith(rel, "/reports/recent")) $("#sm-reports-recent").addClass("on");
      else if (startsWith(rel, "/reports/sma")) $("#sm-reports-sma").addClass("on");
    }
    else if (startsWith(rel, "/cloud")) {
      $("#mm-cloud").addClass("on");
      $("#sm-cloud").addClass("on");
      if (startsWith(rel, "/cloud/metrics")) $("#sm-cloud-metrics").addClass("on");
      else if (startsWith(rel, "/cloud/acquisition")) $("#sm-cloud-acquisition").addClass("on");
    }
  }

  return {
    // Called by page-specific controller scripts to register themselves in single-page app, when page is navigated to.
    registerInitScript: function (pageRel, init) {
      initScripts[pageRel] = init;
    },

    globalInit: function (init) {
      globalInitScripts.push(init);
    },

    navigateTo: function (url) {
      history.pushState(null, null, url);
      dynNavigate();
    }
  };

})();
