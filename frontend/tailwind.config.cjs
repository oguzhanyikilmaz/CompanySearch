/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      fontFamily: {
        sans: ['"Plus Jakarta Sans"', "Segoe UI", "system-ui", "sans-serif"]
      },
      colors: {
        ink: {
          950: "#101418",
          900: "#16212b",
          800: "#24313d",
          700: "#42505d"
        },
        mist: {
          50: "#f7f8f4",
          100: "#eef0e7",
          200: "#dde1d4"
        },
        signal: {
          500: "#1479ff",
          600: "#0c62d1"
        },
        mint: {
          500: "#0f8b6d"
        },
        amber: {
          500: "#d48a16"
        },
        rose: {
          500: "#c75768"
        }
      },
      boxShadow: {
        panel: "0 16px 40px rgba(16, 20, 24, 0.08)"
      }
    }
  },
  plugins: []
};
