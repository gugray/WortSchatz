/// <reference path="../wwwroot/jquery-3.6.0.min.js" />
/// <reference path="../wwwroot/history.min.js" />


// PAGE ================================================

function startsWith(str, prefix) {
  if (str.length < prefix.length)
    return false;
  for (var i = prefix.length - 1; (i >= 0) && (str[i] === prefix[i]); --i)
    continue;
  return i < 0;
}

function escapeHTML(s) {
  return s.replace(/&/g, '&amp;')
          .replace(/"/g, '&quot;')
          .replace(/</g, '&lt;')
          .replace(/>/g, '&gt;');
}

var wsPage = (function () {
  "use strict";

  var reqId = 0; // Current page load request ID. If page has moved on, earlier requests ignored when they complete.
  var location = null; // Full location, as seen in navbar
  var path = null; // Path after domain name
  var rel = null; // Relative path (path without language ID at start)

  // Page init scripts for each page (identified by relPath).
  var initScripts = {};

  // Parse full path, language, and relative path from URL
  function parseLocation() {
    location = window.history.location || window.location;
    var rePath = /https?:\/\/[^\/]+(.*)/i;
    var match = rePath.exec(location);
    path = match[1];
    rel = path;
  }

  function initDebug() {
    var dbgHtml = "<div id='debug'></div>";
    $(dbgHtml).appendTo("body");
    updateDebug();
    $(window).resize(updateDebug);
  }

  function updateDebug() {
    var clientw = document.body.clientWidth;
    var testw = $("#wtest").width();
    var dbgHtml = "ClW: " + clientw + " TestW: " + testw;
    $("#debug").html(dbgHtml);
  }

  function adaptToSize() {
    var clientw = document.body.clientWidth;
    var testw = $("#wtest").width();
    if (clientw <= testw) $("body").addClass("narrow");
    else $("body").removeClass("narrow");
    if (clientw <= testw * 0.6) $("body").addClass("verynarrow");
    else $("body").removeClass("verynarrow");
  }

  // Page just loaded: time to get dynamic part asynchronously, wherever we just landed
  $(document).ready(function () {
    // Debug
    //initDebug();

    // Handcrafted adaptive layout
    adaptToSize();

    // Make sense of location
    parseLocation();
    // Update menu to show where I am (will soon end up being)
    updateMenuState();
    // Overall UI event handlers, including cookie bar
    initGui();
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
        if (data == undefined || data == null) applyFailHtml();
        else dynReady(data, id);
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
    $("#content").addClass("fading");
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
      if (data == undefined || data == null) applyFailHtml();
      else navReady(data, id);
    });
    req.fail(function (jqXHR, textStatus, error) {
      applyFailHtml();
    });
  }

  // Show error content in dynamic area
  function applyFailHtml() {
    $("#content").html("Ouch.");
    // TO-DO: fail title; keywords; description
  }

  // Calls initializers registered for this page.
  function initCurrentPage(data) {
    // Run this page's script initializer, if any
    for (var key in initScripts) {
      if (key == "/") {
        if (rel == "/") initScripts[key](data);
      }
      else if (startsWith(rel, key)) initScripts[key](data);
    }
  }

  // Apply dynamic content: HTML body, title, description, keywords; possible other data
  function applyDynContent(data) {
    if (data.noIndex) $('meta[name=robots]').attr("content", "noindex");
    else $('meta[name=robots]').attr("content", "");
    $(document).attr("title", data.title);
    $("#content").html(data.html);
    $("#content").removeClass("fading");
    // Init page
    initCurrentPage();
    // Scroll to top
    $(window).scrollTop(0);
  }

  function navReady(data, id) {
    // An obsolete request completing too late?
    if (id != reqId) return;

    // Show dynamic content, title etc.
    applyDynContent(data);
    // GA single-page navigation
    ga('set', 'page', path);
    ga('send', {
      hitType: 'pageview',
      page: path,
      title: data.title
    });
  }

  // Dynamic data received after initial page load (not within single-page navigation)
  function dynReady(data, id) {
    // An obsolete request completing too late?
    if (id != -1 && id != reqId) return;

    // Show dynamic content, title etc.
    // Data is null if we're call directly from page load (content already present)
    // BUT: We must still called registered page initializers
    if (data != null) applyDynContent(data);
    else initCurrentPage(data);

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
  }

  // General UI event wireup
  function initGui() {
    // Cookie warning / opt-in pest
    var cookies = localStorage.getItem("cookies");
    if (cookies != "go") $("#bittercookie").css("display", "block");
    $("#swallowbitterpill").click(function (evt) {
      $("#bittercookie").css("display", "none");
      localStorage.setItem("cookies", "go");
      evt.preventDefault();
    });
    // Link to imprint
    $("#imprint").click(function () {
      window.open("/impressum");
    });
  }

  // Updates top navigation menu to reflect where we are
  function updateMenuState() {
    $("#header-menu a").removeClass("selected");
    if (rel == "/" || startsWith(rel, "/ergebnis")) $("#lnkTest").addClass("selected");
    else if (startsWith(rel, "/hintergrund")) $("#lnkBackground").addClass("selected");
  }

  return {
    // Called by page-specific controller scripts to register themselves in single-page app, when page is navigated to.
    registerInitScript: function (pageRel, init) {
      initScripts[pageRel] = init;
    },

    // Retrieves relative URL of current page.
    getRel: function() {
      return rel;
    },

    // Navigates to provided relative URL, respecting single-page navigation.
    navigateTo: function (url) {
      history.pushState(null, null, url);
      dynNavigate();
    }
  };

})();


// QUIZ ================================================

var wsQuiz = (function () {
  "use strict";

  var reqId = 0;
  var words1 = [];
  var words2 = [];

  // Register our init script
  wsPage.registerInitScript("/", init);

  // Serves as "onload", invoked by single-page controller when page loads.
  function init() {
    // "Next" button event handlers
    $(".button").click(onButtonClick);
    $(".button").bind("tap", onButtonClick);
    // Initialize quiz (retrieve or fetch data)
    initQuiz();
  }

  // "Next" button event handler.
  function onButtonClick(evt) {
    if ($(this).hasClass("disabled")) return;
    if ($(this).attr("id") == "donePage01") goToPage02();
    else if ($(this).attr("id") == "donePage02") goToPage03();
    else if ($(this).attr("id") == "donePage03") submitResults();
  }

  // Initialize quiz: load sample if session already has it, or request from server.
  function initQuiz() {
    // Do we have quiz in session storage?
    var storedQuiz = sessionStorage.getItem("quiz");
    if (storedQuiz != null) {
      storedQuiz = JSON.parse(storedQuiz);
      words1 = storedQuiz.words1;
      words2 = storedQuiz.words2;
      displayWords(words1, "#quizTable1");
      $("#donePage01").removeClass("disabled");
      return;
    }
    // Make request
    ++reqId;
    var id = reqId;
    // Submit request
    var req = $.ajax({
      url: "/api/getquiz",
      type: "POST",
      contentType: "application/x-www-form-urlencoded; charset=UTF-8"
    });
    req.done(function (data) {
      if (id != reqId) return;
      words1 = data.words1;
      words2 = data.words2;
      storedQuiz = { words1: words1, words2: words2 };
      sessionStorage.setItem("quiz", JSON.stringify(storedQuiz));
      displayWords(words1, "#quizTable1");
      $("#donePage01").removeClass("disabled");
    });
    req.fail(function (jqXHR, textStatus, error) {
      wsPage.navigateTo("/fehler");
    });
  }

  // Display one page's worth of sample words in table layout.
  function displayWords(words, elmId) {
    var qhtml = "";
    for (var i = 0; i != words.length; ++i) {
      if (i % 4 == 0) {
        //if (i != 0) qhtml += "</div>";
        //qhtml += "<div class='quizRow'>";
      }
      var wix;
      if (i % 4 == 0) wix = i / 4;
      else if (i % 4 == 1) wix = Math.floor(words.length / 4) + Math.floor(i / 4);
      else if (i % 4 == 2) wix = Math.floor(2 * words.length / 4) + Math.floor(i / 4);
      else wix = Math.floor(3 * words.length / 4) + Math.floor(i / 4);
      qhtml += "<div class='quizCell'>" + escapeHTML(words[wix]) + "</div>";
    }
    $(elmId).html(qhtml);
    $(".quizCell").click(onWordClick);
    $(".quizCell").bind("tap", onWordClick);
  }

  // Proceed to second page (broader sample).
  function goToPage02() {
    $("#quiz-first").removeClass("active");
    $("#quiz-second").addClass("active");
    displayWords(words2, "#quizTable2");
    $(window).scrollTop(0);
  }

  // Click/tap event handler: select or unselect word in sample.
  function onWordClick(evt) {
    if ($(this).hasClass("selected")) $(this).removeClass("selected");
    else $(this).addClass("selected");
  }

  // Proceed to third page (optional survey).
  function goToPage03() {
    $("#quiz-second").removeClass("active");
    $("#quiz-third").addClass("active");
    // Initialize survey
    initSurvey();
    $(window).scrollTop(0);
  }

  // Initialize survey's event handlers.
  function initSurvey() {
    $(".quiz-survey-choice").click(onSingleChoiceClick);
    $(".quiz-survey-choice").bind("tap", onSingleChoiceClick);
  }

  // Clears survey input.
  function onClearSurvey() {
    // Remove "selected" class from all choices
    $(".quiz-survey-choice").removeClass("selected");
    // Clear age too
    $("#survAge").val("");
    // Hide optional survey sections
    $("#surveyNonNative").removeClass("visible");
    $("#surveyNative").removeClass("visible");
    // Hide clear command
    $("#quiz-survey-clear").removeClass("visible");
  }

  // Shows and enables "clear survey" command.
  function enableClearSurveyCommand() {
    if ($("#quiz-survey-clear").hasClass("visible")) return;
    $("#quiz-survey-clear").addClass("visible");
    $("#quiz-survey-clear").click(onClearSurvey);
    $("#quiz-survey-clear").bind("tap", onClearSurvey);
  }

  // Event handler: select clicked/tapped item in single-choice answer set.
  function onSingleChoiceClick() {
    // Whatever is clicked, it means we have survey input. Show "clear" command!
    enableClearSurveyCommand();
    // Find parent with quiz-survey-item class: that's our "group"
    var surveyItem = $(this).parents(".quiz-survey-item");
    // Remove "selected" class from all choices
    surveyItem.find(".quiz-survey-choice").removeClass("selected");
    // Make clicked choice selected
    $(this).addClass("selected");
    // Toggle visibility of survey's dynamic sections
    if ($(this).attr("id") == "survNativeYes") {
      $("#surveyNonNative").removeClass("visible");
      $("#surveyNative").addClass("visible");
    }
    if ($(this).attr("id") == "survNativeNo") {
      $("#surveyNative").removeClass("visible");
      $("#surveyNonNative").addClass("visible");
    }
  }

  // Submit all results (quiz and survey); navigate to results page when request finishes.
  function submitResults() {
    $("#donePage03").addClass("disabled");
    // Gather data and counts
    var quiz = gatherQuizInput();
    var survey = gatherSurveyInput();
    var counts = getIncrementCounts("native" in survey);
    // Make request
    ++reqId;
    var id = reqId;
    var data = { quiz: JSON.stringify(quiz), survey: JSON.stringify(survey), quizCount: counts.quizCount, surveyCount: counts.surveyCount };
    // Submit request
    var req = $.ajax({
      url: "/api/evalquiz",
      type: "POST",
      contentType: "application/x-www-form-urlencoded; charset=UTF-8",
      data: data
    });
    req.done(function (data) {
      sessionStorage.removeItem("quiz")
      words1 = words2 = [];
      // Redirect to proper results page
      wsPage.navigateTo("/ergebnis/" + data);
    });
    req.fail(function (jqXHR, textStatus, error) {
      wsPage.navigateTo("/fehler");
    });
    // Track as GA event: quiz submitted
    ga("send", "event", "quiz", "submit");
    plausible("quiz");
  }

  // Gets count of previously submitted quizzes and surveys; increments counts in local storage.
  function getIncrementCounts(hasSurvey) {
    var quizCount = 0;
    var surveyCount = 0;
    if (localStorage.getItem("quizcount") != null) quizCount = localStorage.getItem("quizcount");
    if (localStorage.getItem("surveycount") != null) surveyCount = localStorage.getItem("surveycount");
    var res = { quizCount: quizCount, surveyCount: surveyCount };
    ++quizCount;
    if (hasSurvey) ++surveyCount;
    localStorage.setItem("quizcount", quizCount);
    localStorage.setItem("surveycount", surveyCount);
    return res;
  }

  // Retrieves user input from quiz
  function gatherSurveyInput() {
    var res = {};
    // All single-choice answers
    $(".quiz-survey-choice.selected").each(function () {
      var key = $(this).parent().data("fieldcode");
      var val = $(this).data("rescode");
      res[key] = val;
    });
    // Age
    res["age"] = $("#survAge").val();
    // Done
    return res;
  }

  // Retrieves user input from both quiz tables.
  function gatherQuizInput() {
    var res = [];
    $(".quizTable").find(".quizCell").each(function () {
      var wd = [];
      wd.push($(this).text());
      if ($(this).hasClass("selected")) wd.push("yes");
      else wd.push("no");
      res.push(wd);
    });
    return res;
  }

})();

// RESULT ==============================================

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
      var fbShareHref = "https://facebook.com/sharer/sharer.php?u=https://wortschatz.tk/ergebnis/" + mID[1] + "/share";
      $("#fb-share-link").attr("href", fbShareHref);
      // Update title
      $(document).prop("title", "Meine deutsche Wortschatzgröße ist ungefähr " + data);
      // When share link is clicked: also record GA event
      $("#fb-share-link").click(function () {
        ga("send", "event", "result", "fbshared");
        plausible("result");
      });
    });
    req.fail(function (jqXHR, textStatus, error) {
      wsPage.navigateTo("/fehler");
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

