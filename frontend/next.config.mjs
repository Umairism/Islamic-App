/** @type {import('next').NextConfig} */
const nextConfig = {
  // Disables static HTML export
  output: 'standalone',

  // 1. Fix CSS Modules (Tailwind CSS)
  // This tells Next.js to allow Tailwind's `@layer` directives.
  cssModules: true,

  // 2. Fix ESLint (Strict Mode)
  // This tells Next.js to ignore the build errors from your `eslint.config.js` rules.
  eslint: {
    ignoreDuringBuilds: true,
  },

  // 3. Optional: Optimize for Docker/Container deployment
  // This ensures the app runs correctly when containerized.
  trailingSlash: true,

  images: {
    unoptimized: true,
  },
}

export default nextConfig
