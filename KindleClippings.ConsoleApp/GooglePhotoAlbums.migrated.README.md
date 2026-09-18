# Google Photo Albums migrati

`GooglePhotoAlbums.migrated.xml` è una cartella di lavoro **SpreadsheetML** testuale, apribile direttamente con Microsoft Excel.
È stata usata questa rappresentazione perché la vista delle differenze e la creazione delle pull request non supportano il contenuto binario dei file `.xlsx`.

Il file contiene i fogli:

- `Albums`: una riga per album;
- `AlbumLocations`: una o più località per album, collegate tramite `AlbumId` e ordinate tramite `Order`.

Per ottenere il normale formato `.xlsx`:

1. scaricare `GooglePhotoAlbums.migrated.xml` dalla pull request;
2. aprirlo con Microsoft Excel;
3. scegliere **File → Salva con nome**;
4. selezionare **Cartella di lavoro di Excel (`.xlsx`)**.

Le coordinate sono memorizzate e visualizzate con quattro cifre decimali.
