const { chromium } = require('playwright');

(async () => {

    const browser = await chromium.connectOverCDP(
        'http://127.0.0.1:9222'
    );

    const context = browser.contexts()[0];
    const pages = context.pages();
    const page = pages[pages.length - 1];

    console.log("Album:");
    console.log(
        (await page.title())
            .replace(/ - Google Foto$/, '')
            .replace(/ - Google Photos$/, '')
    );

    const condivisi = page.getByText(
        'Condivisi',
        { exact: true }
    );

    console.log(
        "Elementi 'Condivisi' esatti:",
        await condivisi.count()
    );

    if (!await condivisi.isVisible()) {
        console.log("Condivisi non visibile");
        return;
    }

    console.log("Clicco Condivisi...");
    await condivisi.click();

    await page.waitForTimeout(1000);

    const collabora = page.getByText(
        'Collabora',
        { exact: true }
    );

    console.log(
        "Elementi 'Collabora':",
        await collabora.count()
    );

    if (await collabora.count()) {

        console.log("\nHTML di Collabora:");

        console.log(
            await collabora.first().evaluate(
                node => node.outerHTML
            )
        );

        console.log("\nGENITORE:");

        console.log(
            await collabora.first().evaluate(
                node => node.parentElement?.outerHTML
            )
        );

        console.log("\nNONNO:");

        console.log(
            await collabora.first().evaluate(
                node => node.parentElement?.parentElement?.outerHTML
            )
        );
    }

})();