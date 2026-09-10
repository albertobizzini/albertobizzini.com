const { chromium } = require('playwright');
const fs = require('fs');

const results = [];

const DRY_RUN = true;     // <-- metti false SOLO dopo aver verificato il CSV
const DELAY = 800;

(async () => {

    const browser = await chromium.connectOverCDP(
        'http://127.0.0.1:9222'
    );

    const context = browser.contexts()[0];

    await context.grantPermissions(
        ['clipboard-read', 'clipboard-write'],
        { origin: 'https://photos.google.com' }
    );

    let pages = context.pages();

    const page = pages.length
        ? pages[0]
        : await context.newPage();

    console.log("Apro Google Photos...");

    await page.goto(
        'https://photos.google.com/albums'
    );

    console.log(`
Se richiesto, effettua il login Google.

Quando vedi la pagina degli album, premi INVIO qui.
`);

    await waitEnter();


    // ======================================================
    // RACCOLTA ALBUM
    // ======================================================

    console.log("Raccolgo gli album...");

    const albumUrls = new Set();

    const scrollContainer = page.locator(
        'div.otFYkc.B6Rt6d.zcLWac.eejsDc'
    );

    let stableRounds = 0;
    let previousCount = 0;
    let previousScrollHeight = 0;

    while (stableRounds < 8) {

        const links = await page
            .locator('a')
            .evaluateAll(elements =>
                elements
                    .map(a => a.href)
                    .filter(href =>
                        href &&
                        (
                            href.includes('/album/') ||
                            href.includes('/share/')
                        )
                    )
            );

        links.forEach(x => albumUrls.add(x));

        const currentCount = albumUrls.size;

        const currentScrollHeight =
            await scrollContainer.evaluate(
                el => el.scrollHeight
            );

        console.log(
            `Album trovati: ${currentCount} - ` +
            `scrollHeight: ${currentScrollHeight}`
        );

        if (
            currentCount === previousCount &&
            currentScrollHeight === previousScrollHeight
        ) {
            stableRounds++;
        } else {
            stableRounds = 0;
        }

        previousCount = currentCount;
        previousScrollHeight = currentScrollHeight;

        await scrollContainer.evaluate(el => {
            el.scrollTop = el.scrollHeight;
        });

        await page.waitForTimeout(2000);
    }

    console.log(
        `\nTotale album individuati: ${albumUrls.size}\n`
    );


    // ======================================================
    // ANALISI ALBUM
    // ======================================================

    let checked = 0;
    let collaborative = 0;
    let modified = 0;
    let errors = 0;
    let notShared = 0;

    for (const url of albumUrls) {

        checked++;

        console.log(
            `\n[${checked}/${albumUrls.size}] ${url}`
        );

        let title = '';
        let publicUrl = '';
        let isOn = null;
        let status = '';
        let wasModified = false;
        let dateFrom = '';
        let dateTo = '';

        try {

            await page.goto(url, {
                waitUntil: 'domcontentloaded'
            });

            await page.waitForTimeout(DELAY);


            // --------------------------------------------------
            // TITOLO
            // --------------------------------------------------

            title = (await page.title())
                .replace(/ - Google Foto$/, '')
                .replace(/ - Google Photos$/, '');

            console.log(`Album: ${title}`);


            // --------------------------------------------------
            // DATA / INTERVALLO DATE
            // --------------------------------------------------

            try {

                // L'id #ow15 può essere dinamico: le classi sono più robuste.
                const dateElement = page
                    .locator('div.kk0EU.tN8N7c')
                    .first();

                if (await dateElement.isVisible()) {

                    const rawDate = (await dateElement.innerText()).trim();
                    const parsed = parseAlbumDateRange(rawDate);

                    dateFrom = parsed.dateFrom;
                    dateTo = parsed.dateTo;

                    console.log(
                        `  📅 Data: ${rawDate} -> ${dateFrom || '?'} / ${dateTo || '?'}`
                    );

                } else {

                    console.log('  - Data album non trovata');
                }

            } catch (err) {

                console.log(
                    `  ⚠ Data album non leggibile: ${err.message}`
                );
            }


            // --------------------------------------------------
            // URL PUBBLICO
            // --------------------------------------------------

            try {

                const copyLinkButton =
                    page.getByRole('button', {
                        name: 'Copia link'
                    });

                if (await copyLinkButton.isVisible()) {

                    await copyLinkButton.click();

                    await page.waitForTimeout(300);

                    publicUrl = await page.evaluate(
                        () => navigator.clipboard.readText()
                    );

                    console.log(
                        `  🔗 URL pubblico: ${publicUrl}`
                    );

                } else {

                    console.log(
                        '  - Pulsante "Copia link" non presente'
                    );
                }

            } catch (err) {

                console.log(
                    `  ⚠ URL pubblico non leggibile: ${err.message}`
                );
            }


            // --------------------------------------------------
            // PULSANTE "CONDIVISI"
            // --------------------------------------------------

            const condivisi = page.getByText(
                'Condivisi',
                { exact: true }
            );

            if (!await condivisi.isVisible()) {

                console.log(
                    "  - Album non condiviso"
                );

                notShared++;

                status = 'Non condiviso';

                results.push({
                    title,
                    albumUrl: url,
                    publicUrl,
                    dateFrom,
                    dateTo,
                    collaborative: '',
                    modified: 'NO',
                    status
                });

                continue;
            }


            // --------------------------------------------------
            // APRE LA FINESTRA CONDIVISIONE
            // --------------------------------------------------

            await condivisi.click();

            await page.waitForTimeout(700);


            // --------------------------------------------------
            // SWITCH COLLABORA
            // --------------------------------------------------

            const toggle = page.getByRole(
                'switch',
                { name: 'Collabora' }
            );

            if (!await toggle.isVisible()) {

                console.log(
                    "  ⚠ Switch Collabora non trovato"
                );

                status =
                    'Errore - switch Collabora non trovato';

                results.push({
                    title,
                    albumUrl: url,
                    publicUrl,
                    dateFrom,
                    dateTo,
                    collaborative: '',
                    modified: 'NO',
                    status
                });

                errors++;

                await page.keyboard.press('Escape');

                continue;
            }


            isOn =
                (await toggle.getAttribute(
                    'aria-checked'
                )) === 'true';


            // --------------------------------------------------
            // COLLABORA DISATTIVATO
            // --------------------------------------------------

            if (!isOn) {

                console.log(
                    "  ✓ Collabora già disattivato"
                );

                status =
                    'Condiviso - Collabora disattivato';

                results.push({
                    title,
                    albumUrl: url,
                    publicUrl,
                    dateFrom,
                    dateTo,
                    collaborative: 'NO',
                    modified: 'NO',
                    status
                });

                await page.keyboard.press('Escape');

                continue;
            }


            // --------------------------------------------------
            // COLLABORA ATTIVO
            // --------------------------------------------------

            collaborative++;

            console.log(
                "  🔴 COLLABORA ATTIVO"
            );

            status =
                'Condiviso - Collabora attivo';


            // --------------------------------------------------
            // EVENTUALE MODIFICA
            // --------------------------------------------------

            if (!DRY_RUN) {

                await toggle.click();

                await page.waitForTimeout(500);

                const newState =
                    await toggle.getAttribute(
                        'aria-checked'
                    );

                if (newState === 'false') {

                    modified++;
                    wasModified = true;

                    status =
                        'Condiviso - Collabora disattivato dallo script';

                    console.log(
                        "  → DISATTIVATO"
                    );

                } else {

                    status =
                        'Errore - modifica non confermata';

                    errors++;

                    console.log(
                        "  ⚠ Disattivazione non confermata"
                    );
                }
            }


            // --------------------------------------------------
            // SALVA RISULTATO
            // --------------------------------------------------

            results.push({
                title,
                albumUrl: url,
                publicUrl,
                dateFrom,
                dateTo,
                collaborative: 'SI',
                modified: wasModified ? 'SI' : 'NO',
                status
            });

            await page.keyboard.press('Escape');

        }
        catch (err) {

            errors++;

            console.log(
                "  ❌ ERRORE:",
                err.message
            );

            results.push({
                title: title || '(titolo non rilevato)',
                albumUrl: url,
                publicUrl,
                dateFrom,
                dateTo,
                collaborative: '',
                modified: 'NO',
                status: `Errore - ${err.message}`
            });
        }
    }


    // ======================================================
    // RIEPILOGO
    // ======================================================

    console.log(`
================================

Album individuati  : ${albumUrls.size}
Album controllati  : ${checked}
Album nel CSV       : ${results.length}

Non condivisi       : ${notShared}
Collabora attivo    : ${collaborative}
Modificati          : ${modified}

Errori              : ${errors}

DRY_RUN             : ${DRY_RUN}

================================
`);


    // ======================================================
    // CSV
    // ======================================================

    const csvEscape = value => {

        const s = String(value ?? '');

        return `"${s.replace(/"/g, '""')}"`;
    };


    const csv = [

        [
            'Titolo',
            'URL album',
            'URL pubblico',
            'Data da',
            'Data a',
            'Collabora attivo',
            'Modificato',
            'Stato'
        ]
            .map(csvEscape)
            .join(';'),

        ...results.map(r =>
            [
                r.title,
                r.albumUrl,
                r.publicUrl,
                r.dateFrom,
                r.dateTo,
                r.collaborative,
                r.modified,
                r.status
            ]
                .map(csvEscape)
                .join(';')
        )

    ].join('\r\n');


    fs.writeFileSync(
        'google-photos-report.csv',
        '\ufeff' + csv,
        'utf8'
    );

    console.log(
        '\nCreato google-photos-report.csv'
    );

    console.log(
        `Righe dati CSV: ${results.length}`
    );

    console.log(
        "Browser lasciato aperto per controllo."
    );

})();



function parseAlbumDateRange(value) {

    const months = {
        gen: 1,
        feb: 2,
        mar: 3,
        apr: 4,
        mag: 5,
        giu: 6,
        lug: 7,
        ago: 8,
        set: 9,
        ott: 10,
        nov: 11,
        dic: 12
    };

    const normalized = String(value ?? '')
        .toLowerCase()
        .replace(/[\u2009\u202f\u00a0]/g, ' ')
        .replace(/[–—−]/g, '-')
        .replace(/\s+/g, ' ')
        .trim();

    const formatDate = (day, monthName, year) => {

        const month = months[monthName];

        if (!month || !day || !year) {
            return '';
        }

        return `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
    };

    // Data singola: "2 giu 2015"
    let match = normalized.match(/^(\d{1,2})\s+([a-z]{3})\s+(\d{4})$/);

    if (match) {

        const date = formatDate(
            Number(match[1]),
            match[2],
            Number(match[3])
        );

        return {
            dateFrom: date,
            dateTo: date
        };
    }

    // Intervallo con anno indicato solo alla fine:
    // "21 giu - 07 lug 2018"
    match = normalized.match(
        /^(\d{1,2})\s+([a-z]{3})\s*-\s*(\d{1,2})\s+([a-z]{3})\s+(\d{4})$/
    );

    if (match) {

        const year = Number(match[5]);

        return {
            dateFrom: formatDate(Number(match[1]), match[2], year),
            dateTo: formatDate(Number(match[3]), match[4], year)
        };
    }

    // Intervallo nello stesso mese:
    // "21 - 25 giu 2018"
    match = normalized.match(
        /^(\d{1,2})\s*-\s*(\d{1,2})\s+([a-z]{3})\s+(\d{4})$/
    );

    if (match) {

        const year = Number(match[4]);

        return {
            dateFrom: formatDate(Number(match[1]), match[3], year),
            dateTo: formatDate(Number(match[2]), match[3], year)
        };
    }

    // Intervallo con anno su entrambe le date:
    // "30 dic 2018 - 2 gen 2019"
    match = normalized.match(
        /^(\d{1,2})\s+([a-z]{3})\s+(\d{4})\s*-\s*(\d{1,2})\s+([a-z]{3})\s+(\d{4})$/
    );

    if (match) {

        return {
            dateFrom: formatDate(Number(match[1]), match[2], Number(match[3])),
            dateTo: formatDate(Number(match[4]), match[5], Number(match[6]))
        };
    }

    return {
        dateFrom: '',
        dateTo: ''
    };
}


function waitEnter() {

    return new Promise(resolve => {

        process.stdin.resume();

        process.stdin.once(
            'data',
            () => resolve()
        );
    });
}