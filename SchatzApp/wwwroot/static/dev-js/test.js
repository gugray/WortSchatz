/// <reference path="jquery-2.1.4.min.js" />
/// <reference path="page.js" />

var wsGamme = (function () {
  "use strict";

  var reqId = 0;
  var words1 = [];
  var words2 = [];

  // Register our init script
  wsPage.registerInitScript("/", init);

  function init() {
    // "Next" button event handlers
    $(".button").click(onButtonClick);
    $(".button").bind("tap", onButtonClick);
    // Initialize quiz
    initQuiz();
  }

  function onButtonClick(evt) {
    if ($(this).hasClass("disabled")) return;
    if ($(this).attr("id") == "donePage01") goToPage02();
    else if ($(this).attr("id") == "donePage02") goToPage03();
  }

  function initQuiz() {
    // Do we have quiz in session storage?
    var storedQuiz = sessionStorage.getItem("quiz");
    if (storedQuiz != null) {
      storedQuiz = JSON.parse(storedQuiz);
      words1 = storedQuiz.words1;
      words2 = storedQuiz.words2;
      displayWords(words1, "#quizTable1");
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
    });
  }

  function displayWords(words, elmId) {
    var qhtml = "";
    for (var i = 0; i != words.length; ++i) {
      if (i % 4 == 0) {
        if (i != 0) qhtml += "</div>";
        qhtml += "<div class='quizRow'>";
      }
      qhtml += "<div class='quizCell'>" + escapeHTML(words[i]) + "</div>";
    }
    $(elmId).html(qhtml);
    $(".quizCell").click(onWordClick);
    $(".quizCell").bind("tap", onWordClick);
  }

  function goToPage02() {
    $("#test-first").removeClass("active");
    $("#test-second").addClass("active");
    displayWords(words2, "#quizTable2");
  }

  function onWordClick(evt) {
    if ($(this).hasClass("selected")) $(this).removeClass("selected");
    else $(this).addClass("selected");
  }

  function goToPage03() {
    $("#test-second").removeClass("active");
    $("#test-third").addClass("active");

    //// Disable button while response arrives
    //$("#donePage02").addClass("disabled");
    //// Get text of selected quiz cells
    //var selectedWords = [];
    //$(".quizCell").each(function () {
    //  if ($(this).hasClass("selected")) selectedWords.push($(this).text());
    //});
    //// Make request
    //++reqId;
    //var id = reqId;
    //var data = { name: $("#name-input").val().trim(), words: selectedWords };
    //// Submit request
    //var req = $.ajax({
    //  url: "/api/evalquiz",
    //  type: "POST",
    //  contentType: "application/x-www-form-urlencoded; charset=UTF-8",
    //  data: data
    //});
    //req.done(function (data) {
    //  if (id != reqId) return;
    //  initPage03(data);
    //});
    //req.fail(function (jqXHR, textStatus, error) {
    //  if (id != reqId) return;
    //  $("#donePage02").removeClass("disabled");
    //});
  }

})();
