mergeInto(LibraryManager.library, {
  CryptRushInstallAudioUnlock__deps: ['$WEBAudio'],
  CryptRushInstallAudioUnlock: function () {
    // Some browsers grant audio activation only inside the original input event.
    function resumeAudio() {
      var context = WEBAudio.audioContext;
      if (context && (context.state === 'suspended' || context.state === 'interrupted')) {
        context.resume().catch(function () {
          // A later gesture can retry if the browser is still interrupting playback.
        });
      }
    }
    var gestures = ['keydown', 'pointerup', 'touchend'];
    gestures.forEach(function (type) {
      window.addEventListener(type, resumeAudio, true);
    });
    Module.deinitializers.push(function () {
      gestures.forEach(function (type) {
        window.removeEventListener(type, resumeAudio, true);
      });
    });
  }
});
