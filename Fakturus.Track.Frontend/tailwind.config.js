module.exports = {
  content: [
    "./Pages/**/*.{razor,html,cshtml}",
    "./Layout/**/*.{razor,html,cshtml}",
    "./Shared/**/*.{razor,html,cshtml}",
    "./Components/**/*.{razor,html,cshtml}",
    "./wwwroot/index.html",
    "./**/*.cs"
    ],
    safelist: [
    // Keep essential classes that might be dynamically generated
    'bg-primary-500',
    'bg-secondary-500',
    'text-primary-500',
    'text-secondary-500',
  ],
  theme: {
    extend: {
      screens: {
        'xs': '390px',
      },
      colors: {
        // Primary theme colors - professional and conversion-focused
        primary: {
          DEFAULT: '#2563eb', // Professional blue
          50: '#eff6ff',
          100: '#dbeafe',
          200: '#bfdbfe',
          300: '#93c5fd',
          400: '#60a5fa',
          500: '#2563eb',
          600: '#1d4ed8',
          700: '#1e40af',
          800: '#1e3a8a',
          900: '#1e3a8a',
        },
        secondary: {
          DEFAULT: '#10b981', // Success green
          50: '#ecfdf5',
          100: '#d1fae5',
          200: '#a7f3d0',
          300: '#6ee7b7',
          400: '#34d399',
          500: '#10b981',
          600: '#059669',
          700: '#047857',
          800: '#065f46',
          900: '#064e3b',
        },
        accent: {
          DEFAULT: '#f59e0b', // Call-to-action orange
          50: '#fffbeb',
          100: '#fef3c7',
          200: '#fde68a',
          300: '#fcd34d',
          400: '#fbbf24',
          500: '#f59e0b',
          600: '#d97706',
          700: '#b45309',
          800: '#92400e',
          900: '#78350f',
        }
      },
      fontFamily: {
        'sans': ['Inter', 'system-ui', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Roboto', 'sans-serif'],
      },
      borderRadius: {
        lg: '0.5rem',
        md: '0.375rem',
        sm: '0.25rem',
      },
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('tailwindcss-animate'),
    require('tailwindcss-safe-area')
  ],
}

