const glob = require('glob');
const PurgecssPlugin = require('purgecss-webpack-plugin');
const MiniCssExtractPlugin = require("mini-css-extract-plugin");

module.exports = {
    entry: {
        site: './wwwroot/js/site.js',
        bootstrap_js: './wwwroot/js/bootstrap_js.js',
        validation: './wwwroot/js/validation.js',
        index: './wwwroot/js/index.js'
    },
    output: {
        filename: '[name].entry.js',
        path: __dirname + '/wwwroot/dist'
    },
    devtool: 'source-map',
    mode: 'development',
    module: {
        rules: [
            { test: /\.css$/, use: [{ loader: MiniCssExtractPlugin.loader }, "css-loader"] },
            { test: /\.eot(\?v=\d+\.\d+\.\d+)?$/, loader: "file-loader" },
            { test: /\.(woff|woff2)$/, loader: "url-loader?prefix=font/&limit=5000" },
            { test: /\.ttf(\?v=\d+\.\d+\.\d+)?$/, loader: "url-loader?limit=10000&mimetype=application/octet-stream" },
            { test: /\.svg(\?v=\d+\.\d+\.\d+)?$/, loader: "url-loader?limit=10000&mimetype=image/svg+xml" }
        ]
    },
    plugins: [
            new MiniCssExtractPlugin({
                filename: "[name].css"
            }),//,
            //new PurgecssPlugin({
            //    paths: glob.sync('./Views/**/*.cshtml', { nodir: true })
            //})
        //[Note(2019–04–02): PurgecssPlugin seems to purge too much CSS at present! When working with responsive navbars, the hamburger menu will stop working.There may be a solution in using the correct whitelistPatterns, but adding selectize- (as in this article) doesn’t seem to be enough.For now, omit PurgecssPlugin from the plugins array at the bottom! ]]]
            new PurgecssPlugin({
                    paths: glob.sync('./Views/**/*.cshtml', { nodir: true })
                })
};