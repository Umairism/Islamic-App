import { Analytics } from '@vercel/analytics/next'
import type { Metadata, Viewport } from 'next'
import './globals.css'

export const metadata: Metadata = {
  title: 'The Knowledge Seeker',
  description:
    'Professional Islamic research platform for accessing Quran, Hadith, Tafsir, and Fiqh with advanced search and organization tools.',
  generator: 'The Knowledge Seeker',
  keywords: [
    'Islamic research',
    'Quran',
    'Hadith',
    'Tafsir',
    'Fiqh',
    'Islamic knowledge',
    'Islamic sources',
  ],
  openGraph: {
    title: 'Islamic Research Platform',
    description: 'Professional Islamic research and knowledge management system',
    type: 'website',
  },
  icons: {
    icon: [
      {
        url: '/icon-light-32x32.png',
        media: '(prefers-color-scheme: light)',
      },
      {
        url: '/icon-dark-32x32.png',
        media: '(prefers-color-scheme: dark)',
      },
      {
        url: '/icon.svg',
        type: 'image/svg+xml',
      },
    ],
    apple: '/apple-icon.png',
  },
}

export const viewport: Viewport = {
  colorScheme: 'light dark',
  themeColor: [
    { media: '(prefers-color-scheme: light)', color: '#f8f8f8' },
    { media: '(prefers-color-scheme: dark)', color: '#1f1f1f' },
  ],
  userScalable: true,
  width: 'device-width',
  initialScale: 1,
}

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <html lang="en" dir="ltr" className="bg-background">
      <body className="antialiased text-foreground">
        {children}
        {process.env.NODE_ENV === 'production' && <Analytics />}
      </body>
    </html>
  )
}
