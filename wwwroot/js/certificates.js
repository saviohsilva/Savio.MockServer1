/**
 * certificates.js
 * Funções auxiliares para a página de certificados digitais.
 */

/**
 * Dispara o download de um arquivo a partir de bytes base64.
 * @param {string} base64Data - Conteúdo do arquivo em base64.
 * @param {string} fileName   - Nome do arquivo para o download.
 * @param {string} mimeType   - MIME type do arquivo.
 */
window.downloadBase64File = function (base64Data, fileName, mimeType) {
    const byteChars = atob(base64Data);
    const byteNums = new Array(byteChars.length);
    for (let i = 0; i < byteChars.length; i++) {
        byteNums[i] = byteChars.charCodeAt(i);
    }
    const blob = new Blob([new Uint8Array(byteNums)], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    setTimeout(() => URL.revokeObjectURL(url), 10000);
};
