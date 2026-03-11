mergeInto(LibraryManager.library, {

  Hello: function (param) {
    SendMessageToDB(UTF8ToString(param),"agent")
  },

  StoreEvents: function (param) {
    SendEventToDB(UTF8ToString(param))
  },

  QuitGame: function () {
    CloseApp()
  },

  Print: function (param) {
    console.log(UTF8ToString(param))
  },

});