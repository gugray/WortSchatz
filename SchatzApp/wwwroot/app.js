(() => {
  // src/app.js
  function startsWith(str, prefix) {
    if (str.length < prefix.length)
      return false;
    for (var i = prefix.length - 1; i >= 0 && str[i] === prefix[i]; --i)
      continue;
    return i < 0;
  }
  function escapeHTML(s) {
    return s.replace(/&/g, "&amp;").replace(/"/g, "&quot;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
  }
  var wsPage = function() {
    "use strict";
    var reqId = 0;
    var location = null;
    var path = null;
    var rel = null;
    var initScripts = {};
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
      if (clientw <= testw)
        $("body").addClass("narrow");
      else
        $("body").removeClass("narrow");
      if (clientw <= testw * 0.6)
        $("body").addClass("verynarrow");
      else
        $("body").removeClass("verynarrow");
    }
    $(document).ready(function() {
      adaptToSize();
      parseLocation();
      updateMenuState();
      initGui();
      var hasContent = $("body").hasClass("has-initial-content");
      if (!hasContent) {
        ++reqId;
        var id = reqId;
        var data = { rel };
        var req = $.ajax({
          url: "/api/getpage",
          type: "POST",
          contentType: "application/x-www-form-urlencoded; charset=UTF-8",
          data
        });
        req.done(function(data2) {
          if (data2 == void 0 || data2 == null)
            applyFailHtml();
          else
            dynReady(data2, id);
        });
        req.fail(function(jqXHR, textStatus, error) {
          applyFailHtml();
        });
      }
      if (hasContent)
        dynReady(null, -1);
    });
    function dynNavigate() {
      parseLocation();
      $("#content").addClass("fading");
      updateMenuState();
      ++reqId;
      var id = reqId;
      var data = { rel };
      var req = $.ajax({
        url: "/api/getpage",
        type: "POST",
        contentType: "application/x-www-form-urlencoded; charset=UTF-8",
        data
      });
      req.done(function(data2) {
        if (data2 == void 0 || data2 == null)
          applyFailHtml();
        else
          navReady(data2, id);
      });
      req.fail(function(jqXHR, textStatus, error) {
        applyFailHtml();
      });
    }
    function applyFailHtml() {
      $("#content").html("Ouch.");
    }
    function initCurrentPage(data) {
      for (var key in initScripts) {
        if (key == "/") {
          if (rel == "/")
            initScripts[key](data);
        } else if (startsWith(rel, key))
          initScripts[key](data);
      }
    }
    function applyDynContent(data) {
      if (data.noIndex)
        $("meta[name=robots]").attr("content", "noindex");
      else
        $("meta[name=robots]").attr("content", "");
      $(document).attr("title", data.title);
      $("#content").html(data.html);
      $("#content").removeClass("fading");
      initCurrentPage();
      $(window).scrollTop(0);
    }
    function navReady(data, id) {
      if (id != reqId)
        return;
      applyDynContent(data);
      ga("set", "page", path);
      ga("send", {
        hitType: "pageview",
        page: path,
        title: data.title
      });
    }
    function dynReady(data, id) {
      if (id != -1 && id != reqId)
        return;
      if (data != null)
        applyDynContent(data);
      else
        initCurrentPage(data);
      $(document).on("click", "a.ajax", function() {
        history.pushState(null, null, this.href);
        dynNavigate();
        return false;
      });
      $(window).on("popstate", function(e) {
        dynNavigate();
      });
    }
    function initGui() {
      var cookies = localStorage.getItem("cookies");
      if (cookies != "go")
        $("#bittercookie").css("display", "block");
      $("#swallowbitterpill").click(function(evt) {
        $("#bittercookie").css("display", "none");
        localStorage.setItem("cookies", "go");
        evt.preventDefault();
      });
      $("#imprint").click(function() {
        window.open("/impressum");
      });
    }
    function updateMenuState() {
      $("#header-menu a").removeClass("selected");
      if (rel == "/" || startsWith(rel, "/ergebnis"))
        $("#lnkTest").addClass("selected");
      else if (startsWith(rel, "/hintergrund"))
        $("#lnkBackground").addClass("selected");
    }
    return {
      registerInitScript: function(pageRel, init) {
        initScripts[pageRel] = init;
      },
      getRel: function() {
        return rel;
      },
      navigateTo: function(url) {
        history.pushState(null, null, url);
        dynNavigate();
      }
    };
  }();
  var wsQuiz = function() {
    "use strict";
    var reqId = 0;
    var words1 = [];
    var words2 = [];
    wsPage.registerInitScript("/", init);
    function init() {
      $(".button").click(onButtonClick);
      $(".button").bind("tap", onButtonClick);
      initQuiz();
    }
    function onButtonClick(evt) {
      if ($(this).hasClass("disabled"))
        return;
      if ($(this).attr("id") == "donePage01")
        goToPage02();
      else if ($(this).attr("id") == "donePage02")
        goToPage03();
      else if ($(this).attr("id") == "donePage03")
        submitResults();
    }
    function initQuiz() {
      var storedQuiz = sessionStorage.getItem("quiz");
      if (storedQuiz != null) {
        storedQuiz = JSON.parse(storedQuiz);
        words1 = storedQuiz.words1;
        words2 = storedQuiz.words2;
        displayWords(words1, "#quizTable1");
        $("#donePage01").removeClass("disabled");
        return;
      }
      ++reqId;
      var id = reqId;
      var req = $.ajax({
        url: "/api/getquiz",
        type: "POST",
        contentType: "application/x-www-form-urlencoded; charset=UTF-8"
      });
      req.done(function(data) {
        if (id != reqId)
          return;
        words1 = data.words1;
        words2 = data.words2;
        storedQuiz = { words1, words2 };
        sessionStorage.setItem("quiz", JSON.stringify(storedQuiz));
        displayWords(words1, "#quizTable1");
        $("#donePage01").removeClass("disabled");
      });
      req.fail(function(jqXHR, textStatus, error) {
        wsPage.navigateTo("/fehler");
      });
    }
    function displayWords(words, elmId) {
      var qhtml = "";
      for (var i = 0; i != words.length; ++i) {
        if (i % 4 == 0) {
        }
        var wix;
        if (i % 4 == 0)
          wix = i / 4;
        else if (i % 4 == 1)
          wix = Math.floor(words.length / 4) + Math.floor(i / 4);
        else if (i % 4 == 2)
          wix = Math.floor(2 * words.length / 4) + Math.floor(i / 4);
        else
          wix = Math.floor(3 * words.length / 4) + Math.floor(i / 4);
        qhtml += "<div class='quizCell'>" + escapeHTML(words[wix]) + "</div>";
      }
      $(elmId).html(qhtml);
      $(".quizCell").click(onWordClick);
      $(".quizCell").bind("tap", onWordClick);
    }
    function goToPage02() {
      $("#quiz-first").removeClass("active");
      $("#quiz-second").addClass("active");
      displayWords(words2, "#quizTable2");
      $(window).scrollTop(0);
    }
    function onWordClick(evt) {
      if ($(this).hasClass("selected"))
        $(this).removeClass("selected");
      else
        $(this).addClass("selected");
    }
    function goToPage03() {
      $("#quiz-second").removeClass("active");
      $("#quiz-third").addClass("active");
      initSurvey();
      $(window).scrollTop(0);
    }
    function initSurvey() {
      $(".quiz-survey-choice").click(onSingleChoiceClick);
      $(".quiz-survey-choice").bind("tap", onSingleChoiceClick);
    }
    function onClearSurvey() {
      $(".quiz-survey-choice").removeClass("selected");
      $("#survAge").val("");
      $("#surveyNonNative").removeClass("visible");
      $("#surveyNative").removeClass("visible");
      $("#quiz-survey-clear").removeClass("visible");
    }
    function enableClearSurveyCommand() {
      if ($("#quiz-survey-clear").hasClass("visible"))
        return;
      $("#quiz-survey-clear").addClass("visible");
      $("#quiz-survey-clear").click(onClearSurvey);
      $("#quiz-survey-clear").bind("tap", onClearSurvey);
    }
    function onSingleChoiceClick() {
      enableClearSurveyCommand();
      var surveyItem = $(this).parents(".quiz-survey-item");
      surveyItem.find(".quiz-survey-choice").removeClass("selected");
      $(this).addClass("selected");
      if ($(this).attr("id") == "survNativeYes") {
        $("#surveyNonNative").removeClass("visible");
        $("#surveyNative").addClass("visible");
      }
      if ($(this).attr("id") == "survNativeNo") {
        $("#surveyNative").removeClass("visible");
        $("#surveyNonNative").addClass("visible");
      }
    }
    function submitResults() {
      $("#donePage03").addClass("disabled");
      var quiz = gatherQuizInput();
      var survey = gatherSurveyInput();
      var counts = getIncrementCounts("native" in survey);
      ++reqId;
      var id = reqId;
      var data = { quiz: JSON.stringify(quiz), survey: JSON.stringify(survey), quizCount: counts.quizCount, surveyCount: counts.surveyCount };
      var req = $.ajax({
        url: "/api/evalquiz",
        type: "POST",
        contentType: "application/x-www-form-urlencoded; charset=UTF-8",
        data
      });
      req.done(function(data2) {
        sessionStorage.removeItem("quiz");
        words1 = words2 = [];
        wsPage.navigateTo("/ergebnis/" + data2);
      });
      req.fail(function(jqXHR, textStatus, error) {
        wsPage.navigateTo("/fehler");
      });
      ga("send", "event", "quiz", "submit");
      plausible("quiz");
    }
    function getIncrementCounts(hasSurvey) {
      var quizCount = 0;
      var surveyCount = 0;
      if (localStorage.getItem("quizcount") != null)
        quizCount = localStorage.getItem("quizcount");
      if (localStorage.getItem("surveycount") != null)
        surveyCount = localStorage.getItem("surveycount");
      var res = { quizCount, surveyCount };
      ++quizCount;
      if (hasSurvey)
        ++surveyCount;
      localStorage.setItem("quizcount", quizCount);
      localStorage.setItem("surveycount", surveyCount);
      return res;
    }
    function gatherSurveyInput() {
      var res = {};
      $(".quiz-survey-choice.selected").each(function() {
        var key = $(this).parent().data("fieldcode");
        var val = $(this).data("rescode");
        res[key] = val;
      });
      res["age"] = $("#survAge").val();
      return res;
    }
    function gatherQuizInput() {
      var res = [];
      $(".quizTable").find(".quizCell").each(function() {
        var wd = [];
        wd.push($(this).text());
        if ($(this).hasClass("selected"))
          wd.push("yes");
        else
          wd.push("no");
        res.push(wd);
      });
      return res;
    }
  }();
  var wsResult = function() {
    "use strict";
    var reqId = 0;
    wsPage.registerInitScript("/ergebnis", init);
    function init() {
      var reShare = /\/ergebnis\/.{10}\/share/;
      if (reShare.test(wsPage.getRel())) {
        wsPage.navigateTo("/");
        return;
      }
      var reID = /\/ergebnis\/(.{10})$/;
      var mID = reID.exec(wsPage.getRel());
      ++reqId;
      var id = reqId;
      var data = { uid: mID[1] };
      var req = $.ajax({
        url: "/api/getscore",
        type: "POST",
        contentType: "application/x-www-form-urlencoded; charset=UTF-8",
        data
      });
      req.done(function(data2) {
        if (id != reqId)
          return;
        if (data2 < 9e3)
          roundPrec = 100;
        else
          roundPrec = 500;
        var counter = new CountUp("result-value", 0, data2, 0, 1.5, animOptions);
        counter.start();
        $("#result-link").attr("href", window.location.href);
        $("#result-link").text(window.location.href);
        $("#result-details").addClass("visible");
        var fbShareHref = "https://facebook.com/sharer/sharer.php?u=https://wortschatz.tk/ergebnis/" + mID[1] + "/share";
        $("#fb-share-link").attr("href", fbShareHref);
        $(document).prop("title", "Meine deutsche Wortschatzgr\xF6\xDFe ist ungef\xE4hr " + data2);
        $("#fb-share-link").click(function() {
          ga("send", "event", "result", "fbshared");
          plausible("result");
        });
      });
      req.fail(function(jqXHR, textStatus, error) {
        wsPage.navigateTo("/fehler");
      });
    }
    var roundPrec = 100;
    var easingFn = function(t, b, c, d) {
      var ts = (t /= d) * t;
      var tc = ts * t;
      var res = b + c * (tc + -3 * ts + 3 * t);
      return Math.round(res / roundPrec) * roundPrec;
    };
    var animOptions = {
      useEasing: true,
      easingFn,
      useGrouping: false,
      separator: "",
      decimal: ".",
      prefix: "",
      suffix: ""
    };
  }();
})();
