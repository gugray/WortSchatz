const gulp = require('gulp');
const less = require('gulp-less');
const path = require('path');
const concat = require('gulp-concat');
const plumber = require('gulp-plumber');
const del = require('del');
const uglify = require('gulp-uglify');

// Compile all .less files to .css
gulp.task('less', function () {
  return gulp.src('./wwwroot/static/dev-style/*.less')
    .pipe(plumber())
    .pipe(less({
      paths: [path.join(__dirname, 'less', 'includes')]
    }))
    .pipe(gulp.dest('./wwwroot/static/dev-style/'));
});

// Minify and bundle CSS files
gulp.task('styles', gulp.series('less', function () {
  return gulp.src(['./wwwroot/static/dev-style/*.css', '!./wwwroot/static/dev-style/*.min.css'])
    //.pipe(minifyCSS())
    .pipe(concat('app.min.css'))
    .pipe(gulp.dest('./wwwroot/static/prod-style/'));
}));


// Delete all compiled and bundled files
gulp.task('clean', function () {
  return del(['./wwwroot/static/dev-style/*.css', './wwwroot/static/prod-style/*', './wwwroot/static/prod-js/*']);
});

// Minify and bundle JS files
gulp.task('scripts', function () {
  return gulp.src([
    './wwwroot/static/lib/history.min.js',
    './wwwroot/static/lib/jquery*.min.js',
    './wwwroot/static/lib/countUp.js',
    './wwwroot/static/dev-js/page.js',
    './wwwroot/static/dev-js/quiz.js',
    './wwwroot/static/dev-js/result.js'
  ])
    .pipe(uglify().on('error', function (e) { console.log(e); }))
    .pipe(concat('app.min.js'))
    .pipe(gulp.dest('./wwwroot/static/prod-js/'));
});

// Default task: full clean+build.
gulp.task('default', gulp.series('clean', 'scripts', 'styles'), function () { });

// Watch: recompile less on changes
gulp.task('watch', function () {
  gulp.watch(['./wwwroot/static/dev-style/*.less'], ['less']);
});
