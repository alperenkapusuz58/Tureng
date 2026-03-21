/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./../../../Views/**/*.cshtml",
    "./wwwroot/js/**/*.js",
    "./css/pages/*.scss",
    "../../../../FRONTEND/views/**/*.ejs"
  ],
  theme: {
    extend: {
      container: {
        center:true,
        padding: {
            DEFAULT: '18px',
        },
        screens: {
            sm: '100%',
            md: '100%',
            lg: '1280px',
            xl: '1280px',
        },
      },
      screens: {
        'sm': '640px',
        'md': '960px',
        'lg': '1200px',
        'xl': '1600px',
        'max-lg': {'max': '1350px'},
        'max-md': {'max': '960px'},
        'max-sm': {'max': '640px'},
      },
    },
  },
  plugins: [require("daisyui")],
  daisyui: {
      themes: ["light"],
      base: false,
      styled: true,
      utils: true,
      prefix: "",
      logs: true,
      themeRoot: ":root",
  },
}