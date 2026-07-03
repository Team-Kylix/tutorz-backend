-- ============================================================
-- Commission Rate Backfill Script (Fixed)
-- Purpose: 
--   Step 1: Update existing institutes that still have 0% commission
--           to the default 25% rate.
--   Step 2: Update all classes that belong to an institute but
--           still have InstituteCommissionRate = 0 to use their
--           institute's commission rate.
-- Run: Once after deploying the commission rate feature.
-- ============================================================

-- ── PREVIEW: See what will change ────────────────────────────

-- Institutes with 0% commission (will be set to 25%)
SELECT InstituteId, InstituteName, CommissionPercentage
FROM Institutes
WHERE CommissionPercentage = 0;

-- Classes with institute but 0% commission rate (will be updated)
SELECT 
    c.ClassId,
    c.ClassName,
    c.Subject,
    c.InstituteId,
    i.InstituteName,
    i.CommissionPercentage AS InstituteRate,
    c.InstituteCommissionRate AS CurrentClassRate
FROM Classes c
INNER JOIN Institutes i ON c.InstituteId = i.InstituteId
WHERE c.InstituteId IS NOT NULL
  AND c.InstituteCommissionRate = 0
  AND (c.IsDeleted = 0 OR c.IsDeleted IS NULL);

-- ── STEP 1: Set old institutes to default 25% commission ─────
-- (Institutes created before the feature had CommissionPercentage = 0)
UPDATE Institutes
SET 
    CommissionPercentage = 25,
    UpdatedDate = GETUTCDATE()
WHERE CommissionPercentage = 0;

-- ── STEP 2: Update all classes under any institute to match ──
-- Now all institutes have their correct rate, propagate to classes
UPDATE c
SET 
    c.InstituteCommissionRate = i.CommissionPercentage,
    c.UpdatedDate = GETUTCDATE()
FROM Classes c
INNER JOIN Institutes i ON c.InstituteId = i.InstituteId
WHERE c.InstituteId IS NOT NULL
  AND c.InstituteCommissionRate = 0
  AND (c.IsDeleted = 0 OR c.IsDeleted IS NULL);

-- ── VERIFY: Confirm all institute classes now have a rate ────
SELECT 
    c.ClassId,
    c.ClassName,
    c.Subject,
    i.InstituteName,
    i.CommissionPercentage AS InstituteRate,
    c.InstituteCommissionRate AS ClassCommissionRate,
    CASE 
        WHEN c.InstituteCommissionRate = i.CommissionPercentage THEN 'OK'
        ELSE 'MISMATCH'
    END AS Status
FROM Classes c
INNER JOIN Institutes i ON c.InstituteId = i.InstituteId
WHERE c.InstituteId IS NOT NULL
  AND (c.IsDeleted = 0 OR c.IsDeleted IS NULL)
ORDER BY Status DESC, c.ClassName;
