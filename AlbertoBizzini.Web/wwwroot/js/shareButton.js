window.shareButton = {

    share: async function (data) {

        // Costruisce i dati da condividere.
        // Non passiamo proprietà vuote alla Web Share API.
        const shareData = {};

        if (data.title) {
            shareData.title = data.title;
        }

        if (data.text) {
            shareData.text = data.text;
        }

        if (data.url) {
            shareData.url = data.url;
        }

        // 1. Prova la condivisione nativa.
        //
        // Su dispositivi mobili compatibili:
        // Android -> pannello di condivisione Android
        // iOS     -> pannello di condivisione iOS
        //
        // La chiamata avviene direttamente in risposta al click
        // dell'utente, requisito importante per Web Share API.
        if (navigator.share) {

            try {

                // canShare() verifica, quando disponibile,
                // che i dati possano essere condivisi.
                if (!navigator.canShare || navigator.canShare(shareData)) {

                    await navigator.share(shareData);

                    return {
                        shared: true,
                        copied: false,
                        cancelled: false
                    };
                }
            }
            catch (error) {

                // L'utente ha chiuso il pannello senza condividere.
                if (error && error.name === "AbortError") {

                    return {
                        shared: false,
                        copied: false,
                        cancelled: true
                    };
                }

                console.error(
                    "Errore durante la condivisione:",
                    error
                );
            }
        }

        // 2. Fallback: copia negli appunti.
        //
        // Viene utilizzato, ad esempio, su desktop
        // o su browser che non supportano Web Share API.
        const content = [
            data.title,
            data.text,
            data.url
        ]
            .filter(value => value && value.trim().length > 0)
            .join("\n");

        if (!content) {
            return {
                shared: false,
                copied: false,
                cancelled: false
            };
        }

        try {

            if (navigator.clipboard &&
                navigator.clipboard.writeText) {

                await navigator.clipboard.writeText(content);

                return {
                    shared: false,
                    copied: true,
                    cancelled: false
                };
            }

        }
        catch (error) {

            console.error(
                "Errore durante la copia negli appunti:",
                error
            );
        }

        return {
            shared: false,
            copied: false,
            cancelled: false
        };
    }
};