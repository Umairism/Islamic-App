-- CreateTable
CREATE TABLE "Dataset" (
    "id" TEXT NOT NULL,
    "name" TEXT NOT NULL,
    "edition" TEXT NOT NULL,
    "version" TEXT NOT NULL,
    "source" TEXT NOT NULL,
    "license" TEXT NOT NULL,
    "checksum" TEXT NOT NULL,
    "importedAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "Dataset_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "ImportSession" (
    "id" TEXT NOT NULL,
    "datasetId" TEXT NOT NULL,
    "startedAt" TIMESTAMP(3) NOT NULL,
    "completedAt" TIMESTAMP(3) NOT NULL,
    "status" TEXT NOT NULL,
    "durationMs" INTEGER NOT NULL,
    "warnings" TEXT[],
    "errors" TEXT[],
    "memoryUsageMb" DOUBLE PRECISION NOT NULL,

    CONSTRAINT "ImportSession_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "Surah" (
    "id" TEXT NOT NULL,
    "number" INTEGER NOT NULL,
    "arabicName" TEXT NOT NULL,
    "transliteration" TEXT NOT NULL,
    "englishName" TEXT NOT NULL,
    "revelationType" TEXT NOT NULL,
    "totalVerses" INTEGER NOT NULL,

    CONSTRAINT "Surah_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "QuranVerse" (
    "id" TEXT NOT NULL,
    "globalIndex" INTEGER NOT NULL,
    "surahNumber" INTEGER NOT NULL,
    "ayahNumber" INTEGER NOT NULL,
    "arabicText" TEXT NOT NULL,
    "arabicCleaned" TEXT NOT NULL,
    "transliteration" TEXT NOT NULL,

    CONSTRAINT "QuranVerse_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "QuranTranslation" (
    "id" TEXT NOT NULL,
    "verseId" TEXT NOT NULL,
    "language" TEXT NOT NULL,
    "translator" TEXT NOT NULL,
    "text" TEXT NOT NULL,

    CONSTRAINT "QuranTranslation_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE UNIQUE INDEX "Surah_number_key" ON "Surah"("number");

-- CreateIndex
CREATE UNIQUE INDEX "QuranVerse_globalIndex_key" ON "QuranVerse"("globalIndex");

-- CreateIndex
CREATE INDEX "QuranVerse_surahNumber_ayahNumber_idx" ON "QuranVerse"("surahNumber", "ayahNumber");

-- CreateIndex
CREATE UNIQUE INDEX "QuranVerse_surahNumber_ayahNumber_key" ON "QuranVerse"("surahNumber", "ayahNumber");

-- CreateIndex
CREATE INDEX "QuranTranslation_verseId_language_idx" ON "QuranTranslation"("verseId", "language");

-- AddForeignKey
ALTER TABLE "ImportSession" ADD CONSTRAINT "ImportSession_datasetId_fkey" FOREIGN KEY ("datasetId") REFERENCES "Dataset"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "QuranVerse" ADD CONSTRAINT "QuranVerse_surahNumber_fkey" FOREIGN KEY ("surahNumber") REFERENCES "Surah"("number") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "QuranTranslation" ADD CONSTRAINT "QuranTranslation_verseId_fkey" FOREIGN KEY ("verseId") REFERENCES "QuranVerse"("id") ON DELETE CASCADE ON UPDATE CASCADE;
