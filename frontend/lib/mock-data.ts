/**
 * Mock data fixtures for Islamic Research Platform
 * Simulates API responses - replace with real API calls in production
 */

import {
  QuranVerse,
  Hadith,
  Tafsir,
  Fiqh,
  SourceType,
  LanguageCode,
  HadithAuthenticity,
  Collection,
  SavedSearch,
  SearchQuery,
  SortOption,
} from './types'

// ============================================================================
// Mock Quran Data
// ============================================================================

export const mockQuranVerses: QuranVerse[] = [
  {
    id: 'quran-2-255',
    sourceType: SourceType.QURAN,
    title: 'Ayat al-Kursi (The Throne Verse)',
    titleArabic: 'آية الكرسي',
    description: 'The most famous verse in the Quran',
    descriptionArabic: 'أشهر آية في القرآن الكريم',
    language: LanguageCode.AR,
    author: 'Allah',
    authorArabic: 'الله',
    surahNumber: 2,
    surahName: 'Al-Baqarah',
    surahNameArabic: 'البقرة',
    ayahNumber: 255,
    text: 'Allah - there is no deity except Him, the Ever-Living, the Sustainer of existence. Neither drowsiness overtakes Him nor sleep.',
    textArabic:
      'اللَّهُ لَا إِلَٰهَ إِلَّا هُوَ الْحَيُّ الْقَيُّومُ ۚ لَا تَأْخُذُهُ سِنَةٌ وَلَا نَوْمٌ',
    transliteration: 'Allahu la ilaha illahu, al-hayyu al-qayyumu...',
    translations: [
      {
        language: LanguageCode.EN,
        translator: 'Sahih International',
        text: 'Allah - there is no deity except Him, the Ever-Living, the Sustainer of existence.',
      },
      {
        language: LanguageCode.UR,
        translator: 'Maulana Abul Kalam Azad',
        text: 'اللہ وہ ہے جس کے سوا کوئی معبود نہیں، وہ ہمیشہ زندہ رہنے والا اور سب کو قائم رکھنے والا ہے۔',
      },
    ],
    tags: ['guidance', 'divine-protection', 'theology'],
    citations: 2450,
    relevanceScore: 0.98,
  },
  {
    id: 'quran-1-1-7',
    sourceType: SourceType.QURAN,
    title: 'Surah Al-Fatihah',
    titleArabic: 'سورة الفاتحة',
    description: 'The Opening Chapter',
    descriptionArabic: 'فاتحة الكتاب',
    language: LanguageCode.AR,
    author: 'Allah',
    authorArabic: 'الله',
    surahNumber: 1,
    surahName: 'Al-Fatihah',
    surahNameArabic: 'الفاتحة',
    ayahNumber: 1,
    text: 'In the name of Allah, the Most Gracious, the Most Merciful.',
    textArabic:
      'بِسْمِ اللَّهِ الرَّحْمَٰنِ الرَّحِيمِ',
    transliteration: 'Bismillah ar-Rahman ar-Rahim',
    translations: [
      {
        language: LanguageCode.EN,
        translator: 'Sahih International',
        text: 'In the name of Allah, the Most Gracious, the Most Merciful.',
      },
    ],
    tags: ['opening', 'prayer', 'bismillah'],
    citations: 5230,
    relevanceScore: 0.96,
  },
]

// ============================================================================
// Mock Hadith Data
// ============================================================================

export const mockHadith: Hadith[] = [
  {
    id: 'hadith-bukhari-1',
    sourceType: SourceType.HADITH,
    title: 'The Best Deeds',
    titleArabic: 'أفضل الأعمال',
    description: 'On the best deeds in Islam',
    descriptionArabic: 'في أفضل الأعمال في الإسلام',
    language: LanguageCode.AR,
    text: 'Actions are judged by intentions, and a person will be rewarded according to his intention.',
    textArabic:
      'الأعمال بالنية وإنما لكل امرئ ما نوى',
    narrator: 'Abu Hafs Umar ibn al-Khattab',
    narratorArabic: 'أبو حفص عمر بن الخطاب',
    collection: 'Sahih al-Bukhari',
    bookNumber: 1,
    hadithNumber: 1,
    authenticity: HadithAuthenticity.SAHIH,
    chains: ['Umar ibn al-Khattab', 'Prophet Muhammad'],
    tags: ['intention', 'actions', 'fundamental'],
    citations: 8230,
    relevanceScore: 0.95,
    grades: [
      {
        scholar: 'Al-Bukhari',
        authenticity: HadithAuthenticity.SAHIH,
        notes: 'Classified as Sahih',
      },
      {
        scholar: 'Muslim',
        authenticity: HadithAuthenticity.SAHIH,
        notes: 'Classified as Sahih',
      },
    ],
  },
  {
    id: 'hadith-tirmidhi-1',
    sourceType: SourceType.HADITH,
    title: 'Seeking Knowledge',
    titleArabic: 'طلب العلم',
    description: 'The virtue of seeking Islamic knowledge',
    descriptionArabic: 'فضل طلب العلم الإسلامي',
    language: LanguageCode.AR,
    text: 'Whoever takes a path seeking knowledge, Allah will make easy for him a path to Paradise.',
    textArabic:
      'من سلك طريقا يلتمس فيه علما سهل الله له طريقا إلى الجنة',
    narrator: 'Abu Hurairah',
    narratorArabic: 'أبو هريرة',
    collection: 'Jami\' at-Tirmidhi',
    bookNumber: 45,
    hadithNumber: 2646,
    authenticity: HadithAuthenticity.HASAN,
    chains: ['Abu Hurairah', 'Prophet Muhammad'],
    tags: ['knowledge', 'learning', 'virtue'],
    citations: 4120,
    relevanceScore: 0.92,
    grades: [
      {
        scholar: 'At-Tirmidhi',
        authenticity: HadithAuthenticity.HASAN,
        notes: 'Classified as Hasan',
      },
    ],
  },
]

// ============================================================================
// Mock Tafsir Data
// ============================================================================

export const mockTafsir: Tafsir[] = [
  {
    id: 'tafsir-ibm-2-255',
    sourceType: SourceType.TAFSIR,
    title: 'Commentary on Ayat al-Kursi',
    titleArabic: 'تفسير آية الكرسي',
    description: 'Detailed explanation of Verse 255 from Surah Al-Baqarah',
    descriptionArabic: 'شرح تفصيلي لآية 255 من سورة البقرة',
    language: LanguageCode.AR,
    scholar: 'Ibn Kathir',
    scholarArabic: 'ابن كثير',
    tafsirSchool: 'Salafi',
    text: 'This verse contains the perfect description of Allah\'s attributes. Allah has no drowsiness nor sleep, indicating His constant awareness and vigilance over all creation...',
    textArabic:
      'هذه الآية تحتوي على الأوصاف الكاملة لله تعالى. إن الله لا يغيب عنه شيء ولا يناسيه شيء بخلاف الخلق...',
    quranReference: {
      surahNumber: 2,
      surahName: 'Al-Baqarah',
      ayahStart: 255,
      ayahEnd: 255,
    },
    relatedAyahs: ['2:20', '2:106', '6:3'],
    tags: ['attributes', 'divine-knowledge', 'consciousness'],
    citations: 1230,
    relevanceScore: 0.94,
  },
]

// ============================================================================
// Mock Fiqh Data
// ============================================================================

export const mockFiqh: Fiqh[] = [
  {
    id: 'fiqh-salah-1',
    sourceType: SourceType.FIQH,
    title: 'Pillars of Prayer',
    titleArabic: 'أركان الصلاة',
    description: 'The fundamental conditions for prayer to be valid',
    descriptionArabic: 'الشروط الأساسية لصحة الصلاة',
    language: LanguageCode.AR,
    school: 'Hanafi',
    topic: 'Ritual Prayer',
    topicArabic: 'الصلاة المفروضة',
    text: 'The pillars of prayer are the essential components without which a prayer is invalid. These include: intention, standing (for those able), recitation of Surah Al-Fatihah, bowing, prostration, and sitting between prostrations...',
    textArabic:
      'أركان الصلاة هي العناصر الأساسية التي لا تصح الصلاة بدونها. وتشمل: النية، والقيام لمن استطاع، وقراءة الفاتحة، والركوع، والسجود، والجلوس بين السجدتين...',
    rulings: [
      'Intention is obligatory at the beginning of prayer',
      'Surah Al-Fatihah must be recited in every prayer',
      'Rukoo (bowing) must be performed with dignity',
    ],
    relatedSources: ['quran-2-238', 'hadith-bukhari-757'],
    tags: ['salah', 'worship', 'ritual'],
    citations: 2340,
    relevanceScore: 0.91,
  },
]

// ============================================================================
// Mock Collections
// ============================================================================

export const mockCollections: Collection[] = [
  {
    id: 'coll-1',
    name: 'Daily Quran',
    description: 'Verses for daily reflection and guidance',
    documentIds: ['quran-2-255', 'quran-1-1-7'],
    createdAt: new Date('2024-01-15'),
    updatedAt: new Date('2024-07-10'),
    isPublic: true,
    tags: ['personal', 'daily', 'quran'],
  },
  {
    id: 'coll-2',
    name: 'Hadith on Knowledge',
    description: 'Collected hadith on the importance of knowledge',
    documentIds: ['hadith-tirmidhi-1'],
    createdAt: new Date('2024-02-20'),
    updatedAt: new Date('2024-07-09'),
    isPublic: false,
    tags: ['knowledge', 'learning', 'hadith'],
  },
]

// ============================================================================
// Mock Saved Searches
// ============================================================================

export const mockSavedSearches: SavedSearch[] = [
  {
    id: 'search-1',
    name: 'Recent Hadith',
    query: {
      term: 'hadith',
      sourceTypes: [SourceType.HADITH],
      language: LanguageCode.AR,
      sort: SortOption.DATE_DESC,
      page: 1,
      pageSize: 20,
    },
    createdAt: new Date('2024-06-10'),
    lastRunAt: new Date('2024-07-08'),
  },
]

// ============================================================================
// Mock API Response Functions
// ============================================================================

/**
 * Simulates a search API call
 * Replace this with actual API integration
 */
export function mockSearchDocuments(query: SearchQuery) {
  // Combine all document types
  const allDocuments = [
    ...mockQuranVerses,
    ...mockHadith,
    ...mockTafsir,
    ...mockFiqh,
  ]

  // Filter by source type if specified
  let filtered = allDocuments
  if (query.sourceTypes && query.sourceTypes.length > 0) {
    filtered = filtered.filter((doc) =>
      query.sourceTypes!.includes(doc.sourceType)
    )
  }

  // Filter by search term (simple substring match)
  if (query.term) {
    const term = query.term.toLowerCase()
    filtered = filtered.filter(
      (doc) =>
        doc.title.toLowerCase().includes(term) ||
        doc.text?.toLowerCase().includes(term) ||
        doc.tags.some((tag) => tag.toLowerCase().includes(term))
    )
  }

  // Sort results
  filtered.sort((a, b) => {
    switch (query.sort) {
      case SortOption.RELEVANCE:
        return (b.relevanceScore || 0) - (a.relevanceScore || 0)
      case SortOption.DATE_DESC:
        return (
          new Date(b.createdDate || 0).getTime() -
          new Date(a.createdDate || 0).getTime()
        )
      case SortOption.TITLE_ASC:
        return a.title.localeCompare(b.title)
      default:
        return 0
    }
  })

  // Paginate
  const start = (query.page - 1) * query.pageSize
  const end = start + query.pageSize
  const paginated = filtered.slice(start, end)

  return {
    query,
    total: filtered.length,
    documents: paginated,
    facets: {
      sourceTypes: [
        { type: SourceType.QURAN, count: mockQuranVerses.length },
        { type: SourceType.HADITH, count: mockHadith.length },
        { type: SourceType.TAFSIR, count: mockTafsir.length },
        { type: SourceType.FIQH, count: mockFiqh.length },
      ],
      authenticities: [
        { authenticity: HadithAuthenticity.SAHIH, count: 8 },
        { authenticity: HadithAuthenticity.HASAN, count: 4 },
      ],
      schools: [
        { school: 'Hanafi', count: 12 },
        { school: 'Maliki', count: 8 },
      ],
      authors: [
        { author: 'Ibn Kathir', count: 5 },
        { author: 'At-Tabari', count: 3 },
      ],
      tags: [
        { tag: 'quran', count: 15 },
        { tag: 'hadith', count: 12 },
        { tag: 'fiqh', count: 8 },
      ],
    },
    executionTime: Math.random() * 200 + 50,
  }
}

/**
 * Get a single document by ID
 */
export function mockGetDocument(id: string) {
  const allDocuments = [
    ...mockQuranVerses,
    ...mockHadith,
    ...mockTafsir,
    ...mockFiqh,
  ]
  return allDocuments.find((doc) => doc.id === id)
}

/**
 * Get all collections
 */
export function mockGetCollections() {
  return mockCollections
}

/**
 * Get all saved searches
 */
export function mockGetSavedSearches() {
  return mockSavedSearches
}
