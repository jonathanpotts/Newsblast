/// <binding BeforeBuild='compile:sass' AfterBuild='min:css' Clean='clean:css' ProjectOpened='watch:sass' />
/*
This file is the main entry point for defining Gulp tasks and using Gulp plugins.
Click here to learn more. https://go.microsoft.com/fwlink/?LinkId=518007
*/

var gulp = require("gulp");
var sass = require("gulp-sass");
var cssmin = require("gulp-cssmin");
var concat = require("gulp-concat");
var rename = require("gulp-rename");
var del = require("del");
var pump = require("pump");

var paths = {
    webroot: "./wwwroot/",
    sassDir: "./Styles/"
};

paths.cssDir = paths.webroot + "css/";
paths.sassFiles = paths.sassDir + "**/*.scss";
paths.cssFiles = paths.cssDir + "**/*.css";
paths.minCssFiles = paths.cssDir + "**/*.min.css";

var bundleNames = {
    css: "site.bundle.css",
};

paths.cssBundleFile = paths.cssDir + bundleNames.css;

gulp.task("compile:sass", function () {
    return gulp.src(paths.sassFiles)
        .pipe(sass().on("error", sass.logError))
        .pipe(gulp.dest(paths.cssDir));
});

gulp.task("min:css", function () {
    return gulp.src([paths.cssFiles, "!" + paths.minCssFiles])
        .pipe(cssmin())
        .pipe(rename({ suffix: ".min" }))
        .pipe(gulp.dest(paths.cssDir));
});

gulp.task("clean:css", function () {
    return del(paths.cssFiles);
});

gulp.task("watch:sass", function () {
    return gulp.watch(paths.sassFiles, ["compile:sass"]);
});