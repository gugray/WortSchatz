const esbuild = require('esbuild');
const { lessLoader } = require('esbuild-plugin-less');
const livereload = require("livereload");

const buildOptions = {
  entryPoints: ['src/app.js', "src/app.less"],
  outdir: 'wwwroot',
}

const args = (argList => {
  let res = {};
  let opt, thisOpt, curOpt;
  for (let i = 0; i < argList.length; i++) {
    thisOpt = argList[i].trim();
    opt = thisOpt.replace(/^\-+/, '');
    if (opt === thisOpt) {
      // argument value
      if (curOpt) res[curOpt] = opt;
      curOpt = null;
    }
    else {
      // argument name
      curOpt = opt;
      res[curOpt] = true;
    }
  }
  return res;
})(process.argv);

let watch = false;

if (args.watch) {
  watch = {
    onRebuild(error) {
      var dstr = "[" + new Date().toLocaleTimeString() + "] ";
      if (error) {
        console.error(dstr + 'Change detected; rebuild failed:', error);
        return;
      }
      console.log(dstr + 'Change detected; rebuild OK');
    },
  };
}

esbuild.build({
  entryPoints: buildOptions.entryPoints,
  outdir: buildOptions.outdir,
  bundle: true,
  plugins: [lessLoader()],
  watch: watch,
}).catch(err => {
  console.error("Unexpected error; quitting.");
  if (err) console.error(err);
  process.exit(1);
}).then(() => {
  console.log("Build finished.");
  if (args.watch) {
    livereload.createServer().watch(buildOptions.outdir);
    console.log("Watching changes, with livereload...");
  }
});

