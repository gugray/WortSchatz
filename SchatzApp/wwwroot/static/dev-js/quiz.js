/// <reference path="../lib/jquery-2.1.4.min.js" />
/// <reference path="page.js" />

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
  }

  // Display one page's worth of sample words in table layout.
  function displayWords(words, elmId) {
    var qhtml = "";
    for (var i = 0; i != words.length; ++i) {
      if (i % 4 == 0) {
        if (i != 0) qhtml += "</div>";
        qhtml += "<div class='quizRow'>";
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
    // Track as GA event: quiz submitted
    ga("send", "event", "quiz", "submit");
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
