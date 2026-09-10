const { chromium } = require('playwright');

(async () => {

    const browser = await chromium.connectOverCDP(
        'http://127.0.0.1:9222'
    );

    const context = browser.contexts()[0];
    const pages = context.pages();
    const page = pages[pages.length - 1];

    console.log("URL:", page.url());

    const scrollables = await page.evaluate(() => {

        const all = [...document.querySelectorAll('*')];

        return all
            .map((el, index) => {
                const style = getComputedStyle(el);

                return {
                    index,
                    tag: el.tagName,
                    cls: el.className,
                    scrollHeight: el.scrollHeight,
                    clientHeight: el.clientHeight,
                    overflowY: style.overflowY
                };
            })
            .filter(x =>
                x.scrollHeight > x.clientHeight + 100 &&
                (
                    x.overflowY === 'auto' ||
                    x.overflowY === 'scroll'
                )
            )
            .sort((a, b) =>
                b.scrollHeight - a.scrollHeight
            );
    });

    console.log(scrollables.slice(0, 20));

})();