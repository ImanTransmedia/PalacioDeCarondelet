mergeInto(LibraryManager.library, {
  DS_GetDeviceString: function () {
    var ua = (navigator.userAgent || "").toLowerCase();

    var maxTouch = 0;
    try { maxTouch = navigator.maxTouchPoints || 0; } catch (e) {}

    var isMacLike = ua.indexOf("macintosh") !== -1;
    var isIpadOS = isMacLike && maxTouch > 1;

    var isIphone = ua.indexOf("iphone") !== -1;
    var isIpad = ua.indexOf("ipad") !== -1 || isIpadOS;
    var isAndroid = ua.indexOf("android") !== -1;
    var isMobileKeyword = ua.indexOf("mobile") !== -1;

    var isWindows = ua.indexOf("windows") !== -1;
    var isMac = (ua.indexOf("mac os x") !== -1 || isMacLike) && !isIpadOS;

    var isAndroidTablet = isAndroid && !isMobileKeyword;
    var isAndroidPhone = isAndroid && isMobileKeyword;

    var result = "PC";

    if (isIphone) result = "iOS (iPhone)";
    else if (isIpad) result = "iOS (iPad)";
    else if (isAndroidPhone) result = "Android (Phone)";
    else if (isAndroidTablet) result = "Android (Tablet)";
    else if (isWindows) result = "PC (Windows)";
    else if (isMac) result = "PC (Mac)";
    else {
      if (maxTouch > 1 && (isMobileKeyword || isAndroid || isIpadOS)) result = "Mobile";
      else result = "PC";
    }

    if (typeof stringToNewUTF8 === "function") {
      return stringToNewUTF8(result);
    }

    var size = (typeof lengthBytesUTF8 === "function")
      ? lengthBytesUTF8(result) + 1
      : (result.length * 4) + 1;

    var ptr = _malloc(size);
    stringToUTF8(result, ptr, size);
    return ptr;
  },

  DS_IsTouchDevice: function () {
    try { return (navigator.maxTouchPoints || 0) > 0 ? 1 : 0; }
    catch (e) { return 0; }
  }
});
