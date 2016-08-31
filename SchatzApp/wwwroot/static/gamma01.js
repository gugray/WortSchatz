/// <reference path="x-jquery-2.1.4.min.js" />

var wsGamme = (function () {
  "use strict";

  var reqId = 0;

  $(document).ready(function () {
    $(".button").click(onButtonClick);
    $(".button").bind("tap", onButtonClick);
    $("#name-input").select();
    $("#name-input").focus();
    $("#name-input").on("input", updateButtonP01);
  });

  function updateButtonP01() {
    if ($("#name-input").val().trim() == "") $("#donePage01").addClass("disabled");
    else $("#donePage01").removeClass("disabled");
  }

  function onButtonClick(evt) {
    if ($(this).hasClass("disabled")) return;
    if ($(this).attr("id") == "donePage01") goToPage02();
    else if ($(this).attr("id") == "donePage02") goToPage03();
  }

  function goToPage02() {
    // Disable button while response arrives
    $("#donePage01").addClass("disabled");
    // Make request
    ++reqId;
    var id = reqId;
    // Submit request
    var req = $.ajax({
      url: "/api/getgammaquiz",
      type: "POST",
      contentType: "application/x-www-form-urlencoded; charset=UTF-8"
    });
    req.done(function (data) {
      if (id != reqId) return;
      initPage02(data);
    });
    req.fail(function (jqXHR, textStatus, error) {
      if (id != reqId) return;
      $("#donePage01").removeClass("disabled");
    });
  }

  function initPage02(data) {
    $(".quizTable").html(data);
    $("#wiz01").removeClass("active");
    $("#wiz02").addClass("active");
    $(".quizCell").click(onWordClick);
    $(".quizCell").bind("tap", onWordClick);
  }

  function onWordClick(evt) {
    if ($(this).hasClass("selected")) $(this).removeClass("selected");
    else $(this).addClass("selected");
  }

  function goToPage03() {
    // Disable button while response arrives
    $("#donePage02").addClass("disabled");
    // Get text of selected quiz cells
    var selectedWords = [];
    $(".quizCell").each(function () {
      if ($(this).hasClass("selected")) selectedWords.push($(this).text());
    });
    // Make request
    ++reqId;
    var id = reqId;
    var data = { name: $("#name-input").val().trim(), words: selectedWords };
    // Submit request
    var req = $.ajax({
      url: "/api/evalquiz",
      type: "POST",
      contentType: "application/x-www-form-urlencoded; charset=UTF-8",
      data: data
    });
    req.done(function (data) {
      if (id != reqId) return;
      initPage03(data);
    });
    req.fail(function (jqXHR, textStatus, error) {
      if (id != reqId) return;
      $("#donePage02").removeClass("disabled");
    });

  }

  function initPage03(data) {
    $("#wiz02").removeClass("active");
    $("#wiz03").addClass("active");
    $("#scoreProp").text(data.scoreProp);
    $("#scoreMean").text(data.scoreMean);
  }

})();
