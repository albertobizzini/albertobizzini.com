const { chromium } = require('playwright');

(async () => {

    const browser = await chromium.connectOverCDP(
        'http://127.0.0.1:9222'
    );

    const context = browser.contexts()[0];

    const pages = context.pages();

    // usa la scheda attualmente aperta
    const page = pages[pages.length - 1];

    console.log("URL:");
    console.log(page.url());

    console.log("\nTITOLO PAGINA:");
    console.log(await page.title());

    console.log("\nELEMENTI CONTENENTI 'Condivisi':");

    const shared = page.getByText(/Condivisi/i);

    console.log("Numero trovati:", await shared.count());

    for (let i = 0; i < await shared.count(); i++) {
        const el = shared.nth(i);

        console.log(`\n--- elemento ${i + 1} ---`);

        console.log(
            await el.evaluate(node => node.outerHTML)
        );
    }

    console.log("\nPULSANTI / ELEMENTI CLICCABILI:");

    const clickable = page.locator(
        'button, [role="button"], a'
    );

    const count = await clickable.count();

    for (let i = 0; i < Math.min(count, 100); i++) {

        const el = clickable.nth(i);

        const txt = (
            await el.innerText().catch(() => '')
        ).trim();

        const aria =
            await el.getAttribute('aria-label');

        if (txt || aria) {
            console.log(
                `[${i}] testo="${txt}" aria="${aria}"`
            );
        }
    }

})();