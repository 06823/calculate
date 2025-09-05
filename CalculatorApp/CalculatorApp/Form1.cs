using System;
using System.Windows.Forms;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CalculatorApp
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// 最初の値を保持する変数
        /// </summary>
        private decimal firstValue = 0;

        /// <summary>
        /// 2番目の値を保持する変数
        /// </summary>
        private decimal secondValue = 0;

        /// <summary>
        /// テキストボックスの上書きモードを示すフラグ
        /// </summary>
        private bool text_overwrite = false;

        /// <summary>
        /// 加算演算子の記号
        /// </summary>
        private const string AddSymbol = "+";

        /// <summary>
        /// 減算演算子の記号
        /// </summary>
        private const string SubtractSymbol = "-";

        /// <summary>
        /// 乗算演算子の記号
        /// </summary>
        private const string MultiplySymbol = "×";

        /// <summary>
        /// 除算演算子の記号
        /// </summary>
        private const string DivideSymbol = "÷";

        /// <summary>
        /// 等号演算子の記号
        /// </summary>
        private const string EqualSymbol = "=";

        /// <summary>
        /// 初期値：0
        /// </summary>
        private const decimal InitialValue = 0m;

        /// <summary>
        /// 表示値：0
        /// </summary>
        private const string ZeroValue = "0";

        /// <summary>
        /// 小数点が入力されているかを示すフラグ
        /// </summary>
        private bool Num_Dot = false;


        private float defaultFontSize;
        private bool isErrorState = false;
        private bool isClearEntry = false;
        private decimal displayValue = 0;

        // フォントサイズ調整用
        private const int MaxVisibleDigits = 11;
        private const float MinFontSize = 18.0f;
        private const float FontSizeDecrement = 2.1f;

        // 指数表記用の最大表示桁数
        private const int MaxExponentialDigits = 13;

        // 指数表記に切り替える桁数
        private const int MaxDigitsForExponential = 16;

        //エラー時に操作無効なキー
        private Button[] DisabledButtonsOnError;

        private enum OperatorType
        {
            /// <summary>演算なし</summary>
            NON,
            /// <summary>加算</summary>
            ADD,
            /// <summary>減算</summary>
            SUBTRACT,
            /// <summary>乗算</summary>
            MULTIPLY,
            /// <summary>除算</summary>
            DIVIDE,
            /// <summary>パーセント</summary>
            PERCENT
        }

        /// <summary>
        /// 現在の演算子のタイプを保持する変数
        /// </summary>
        private OperatorType mType = OperatorType.NON;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            DisabledButtonsOnError =
            new Button[]
            {
                btnDot,btnTogglesign,btnPercent,btnPlus,btnMinus,btnMultiply,btnDivide
            };
        }

        /// <summary>
        /// フォームの初期設定
        /// </summary>
        private void Form1_Load(object sender, EventArgs e)
        {
            textResult.Text = ZeroValue;
            text_overwrite = true;
            defaultFontSize = textResult.Font.Size;

            // 計算結果表示欄設定（読み取り専用、右揃え、ボーダースタイルなし）
            textResult.ReadOnly = true;
            textResult.TextAlign = HorizontalAlignment.Right;
            textResult.BorderStyle = BorderStyle.None;

            // 途中結果表示欄設定（読み取り専用、右揃え、ボーダースタイルなし）
            textExpression.ReadOnly = true;
            textExpression.TextAlign = HorizontalAlignment.Right;
            textExpression.BorderStyle = BorderStyle.None;
        }

        /// <summary>
        /// 計算機の状態をリセット
        /// </summary>
        private void ResetCalculatorState()
        {
            firstValue = 0;
            secondValue = 0;
            mType = OperatorType.NON;
            textExpression.Text = "";
            textResult.Text = ZeroValue;
            text_overwrite = true;
            Num_Dot = false;
            isErrorState = false;
            isClearEntry = false;
            displayValue = 0;
            textResult.Font = new Font(textResult.Font.FontFamily, defaultFontSize, textResult.Font.Style);
        }


        private void SetButtonsEnabled(bool enabled)
        {
            foreach (var btn in DisabledButtonsOnError)
            {
                btn.Enabled = enabled;
            }
        }


        /// <summary>
        /// 指定された演算子タイプに基づいて演算子の記号を取得する
        /// </summary>
        /// <param name="type">演算子のタイプ</param>
        /// <returns>指定された演算子タイプに対応する記号</returns>
        private string GetOperatorSymbol(OperatorType type)
        {
            switch (type)
            {
                case OperatorType.ADD:
                    return AddSymbol;
                case OperatorType.SUBTRACT: 
                    return SubtractSymbol;
                case OperatorType.MULTIPLY:
                    return MultiplySymbol;
                case OperatorType.DIVIDE: 
                    return DivideSymbol;
                default: 
                    return string.Empty;
            }
        }

        /// <summary>
        /// 計算を実行
        /// </summary>
        private decimal Calculate(decimal left, decimal right, OperatorType type)
        {
            switch (type)
            {
                case OperatorType.ADD:
                    return left + right;
                case OperatorType.SUBTRACT:
                    return left - right;
                case OperatorType.MULTIPLY:
                    return left * right;
                case OperatorType.DIVIDE:
                    return left / right;
                default:
                    return right;
            }
        }

        /// <summary>
        /// 演算キーの処理
        /// </summary>
        private void btnOperation_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            OperatorType operationType = OperatorType.NON;

            switch (btn.Text)
            {
                case AddSymbol:
                    operationType = OperatorType.ADD;
                    break;
                case SubtractSymbol:
                    operationType = OperatorType.SUBTRACT;
                    break;
                case MultiplySymbol:
                    operationType = OperatorType.MULTIPLY;
                    break;
                case DivideSymbol:
                    operationType = OperatorType.DIVIDE;
                    break;
            }
            HandleOperatorClick(operationType);
        }

        /// <summary>
        /// 演算子ボタンクリック時のメイン処理を統括。
        /// </summary>
        /// <param name="operationType">実行する演算子のタイプ</param>
        private void HandleOperatorClick(OperatorType operationType)
        {
            if (isErrorState)
            {
                ResetCalculatorState();
                return;
            }

            try
            {
                if (isClearEntry)
                {
                    mType = operationType;
                    string operatorSymbol = GetOperatorSymbol(mType);
                    textExpression.Text = string.Format("{0} {1}", FormatNumberForExpression(firstValue), operatorSymbol);
                    textResult.Text = FormatNumberForDisplay(firstValue);
                    UpdateTextResultWithCommas();
                    isClearEntry = false;
                    text_overwrite = true;
                   
                    return;
                }

                // 演算子が連続して入力された場合の新しい処理
                if (text_overwrite && mType != OperatorType.NON && !textExpression.Text.EndsWith(EqualSymbol))
                {
                    mType = operationType;
                    UpdateExpressionDisplay(firstValue, mType);
                }
                else
                {
                    decimal currentValue = GetCurrentValue();

                    PerformPendingCalculation(currentValue);
                    UpdateTextResultWithCommas(); 

                    if (isErrorState) return;


                    textResult.Text = FormatNumberForDisplay(firstValue);



                    mType = operationType;
                    UpdateExpressionDisplay(firstValue, mType);
                }

                text_overwrite = true;
                Num_Dot = textResult.Text.Contains(".");
               
            }
            catch (OverflowException)
            {
                SetErrorState("計算範囲を超えました");
            }
        }

        /// <summary>
        /// 前の演算子と現在の値を使用して計算を実行し、firstValue
        /// </summary>
        /// <param name="currentValue">現在の表示値</param>
        private void PerformPendingCalculation(decimal currentValue)
        {
            if (textExpression.Text.EndsWith("=") || mType == OperatorType.NON)
            {
                firstValue = currentValue;
            }
            else
            {
                // ゼロ除算のチェック
                if (mType == OperatorType.DIVIDE && currentValue == 0)
                {
                    SetErrorState("0で割ることはできません");
                    return;
                }

                decimal result = Calculate(firstValue, currentValue, mType);
                firstValue = result;
            }
        }

        /// <summary>
        /// 計算式表示欄のテキストを更新
        /// </summary>
        /// <param name="value">表示する値</param>
        /// <param name="type">演算子のタイプ</param>
        private void UpdateExpressionDisplay(decimal value, OperatorType type)
        {
            string operatorSymbol = GetOperatorSymbol(type);
            textExpression.Text = string.Format("{0} {1}", FormatNumberForExpression(value), operatorSymbol);
        }

        /// <summary>
        /// エラー状態を設定し、表示を更新
        /// </summary>
        /// <param name="message">表示するエラーメッセージ</param>
        private void SetErrorState(string message)
        {
            textResult.Text = message;
            textResult.Font = new Font(textResult.Font.FontFamily, 20, textResult.Font.Style);
            isErrorState = true;
            SetButtonsEnabled(false);
        }

        /// <summary>
        /// 数字キーが押されたときの処理
        /// </summary>
        private void btnNum_Click(object sender, EventArgs e)
        {
            string digit = GetDigitFromButton(sender);
            HandleInitialState();
            HandleNumericButtonClick(digit);
        }

        private string GetDigitFromButton(object sender)
        {
            // object型をButton型に変換する
            // as演算子を使用すると、変換に失敗した場合にnullが返される
            Button btn = sender as Button;

            // btnがnullではないかチェックし、nullの場合は"0"を返す
            if (btn != null)
            {
                return btn.Text;
            }
            else
            {
                return ZeroValue;
            }
        }

        /// <summary>
        /// 数字ボタンクリック時のメイン処理
        /// </summary>
        /// <param name="digit">入力された数字の文字列</param>
        private void HandleNumericButtonClick(string digit)
        {
            HandleInitialState();
            SetButtonsEnabled(true);

            string currentText = textResult.Text.Replace(",", "");

            // 入力の有効性をチェック
            if (!IsInputValid(currentText, digit))
            {
                return;
            }

            // 表示を更新
            UpdateDisplayWithNewDigit(currentText, digit);

            // 状態を更新
            isClearEntry = false;
           
        }

        /// <summary>
        /// 計算機が初期状態にあるかを確認し、必要に応じてリセット
        /// </summary>
        private void HandleInitialState()
        {
            if (isErrorState || textExpression.Text.EndsWith(EqualSymbol))
            {
                ResetCalculatorState();
            }
        }

        /// <summary>
        /// 入力された数字が有効かどうかを検証
        /// </summary>
        /// <param name="currentText">現在のテキスト</param>
        /// <param name="digit">入力された数字</param>
        /// <returns>有効な場合は true</returns>
        private bool IsInputValid(string currentText, string digit)
        {
            bool startsWithZeroDot = currentText.StartsWith("0.") || currentText.StartsWith("-0.");
            int maxDigits = startsWithZeroDot ? 17 : 16;

            string nextText = text_overwrite ? digit : currentText + digit;
            int nextLength = nextText.Replace(".", "").Replace("-", "").Length;

            if (nextLength > maxDigits)
            {
                return false;
            }

            if (!text_overwrite && currentText == ZeroValue && digit == ZeroValue && !Num_Dot)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 新しい数字で表示を更新
        /// </summary>
        /// <param name="currentText">現在のテキスト</param>
        /// <param name="digit">入力された数字</param>
        private void UpdateDisplayWithNewDigit(string currentText, string digit)
        {
            if (text_overwrite)
            {
                textResult.Text = digit;
                text_overwrite = false;
                Num_Dot = textResult.Text.Contains(".");
            }
            else
            {
                textResult.Text += digit;
            }
            UpdateTextResultWithCommas();
        }

        /// <summary>
        /// 小数点キーが押されたときの処理
        /// </summary>
        private void btnDot_Click(object sender, EventArgs e)
        {
            HandleInitialState();
            InsertDecimalPoint();
         
        }

        
        private void InsertDecimalPoint()
        {
            if (Num_Dot)
                return;

            if (text_overwrite)
            {
                textResult.Text = "0.";
                text_overwrite = false;
            }
            else
            {
                textResult.Text += ".";
            }
            Num_Dot = true;
        }

        /// <summary>
        /// パーセントキーが押されたときの処理
        /// </summary>
        private void btnPercent_Click(object sender, EventArgs e)
        {
            if (ShouldResetOnError())
                return;

            if (mType == OperatorType.NON)
            {
                SetPercentDefaultDisplay();
                return;
            }

            decimal currentValue = GetCurrentValue();
            decimal percentValue = CalculatePercent(currentValue);
            UpdatePercentDisplay(percentValue);
            
        }

        // エラー判定
        private bool ShouldResetOnError()
        {
            if (isErrorState)
            {
                ResetCalculatorState();
                return true;
            }
            return false;
        }

        // パーセント計算
        private decimal CalculatePercent(decimal value)
        {
            return value * 0.01m;
        }

        // 式・表示更新
        private void UpdatePercentDisplay(decimal percentValue)
        {
            decimal previousValue = firstValue;
            if (mType == OperatorType.ADD || mType == OperatorType.SUBTRACT)
            {
                decimal calculatedValue = previousValue * percentValue;
                textExpression.Text = string.Format("{0} {1} {2}", FormatNumberForExpression(previousValue), GetOperatorSymbol(mType), FormatNumberForExpression(calculatedValue));
                textResult.Text = FormatNumberForDisplay(calculatedValue);
            }
            else
            {
                textExpression.Text = string.Format("{0} {1} {2}", FormatNumberForExpression(previousValue), GetOperatorSymbol(mType), FormatNumberForExpression(percentValue));
                textResult.Text = FormatNumberForDisplay(percentValue);
            }
            text_overwrite = true;
            Num_Dot = textResult.Text.Contains(".");
        }

        // パーセント初期表示
        private void SetPercentDefaultDisplay()
        {
            textResult.Text = ZeroValue;
            textExpression.Text = ZeroValue;
        }

        /// <summary>
        /// イコールキーが押されたときの処理
        /// </summary>
        private void btnEnter_Click(object sender, EventArgs e)
        {
            HandleEqualsClick();
        }

        private void HandleEqualsClick()
        {
            if (ShouldResetOnError())
                return;

            try
            {
                decimal result = ProcessEqualsLogic();
                if (isErrorState) return;
                UpdateDisplayAfterCalculation(result);
            }
            catch (InvalidOperationException ex)
            {
                HandleCalculationException(ex.Message);
            }
            catch (OverflowException)
            {
                HandleCalculationException("計算範囲を超えました");
            }
        }

        private decimal ProcessEqualsLogic()
        {
            decimal currentValue = GetCurrentValue();
            bool isFirstEqual = !textExpression.Text.EndsWith(EqualSymbol);

            if (mType == OperatorType.NON)
            {
                secondValue = currentValue;
                textExpression.Text = string.Format("{0} =", FormatNumberForExpression(currentValue));
                return currentValue;
            }

            decimal left = isFirstEqual ? firstValue : currentValue;
            decimal right = isFirstEqual ? currentValue : secondValue;

            // ゼロ除算のチェック
            if (mType == OperatorType.DIVIDE && right == 0)
            {
                throw new InvalidOperationException("0で割ることはできません");
            }

            decimal result = Calculate(left, right, mType);

            if (isFirstEqual)
            {
                secondValue = currentValue;
                textExpression.Text = string.Format("{0} {1} {2} =", FormatNumberForExpression(firstValue), GetOperatorSymbol(mType), FormatNumberForExpression(secondValue));
            }
            else
            {
                textExpression.Text = string.Format("{0} {1} {2} =", FormatNumberForExpression(currentValue), GetOperatorSymbol(mType), FormatNumberForExpression(secondValue));
            }

            return result;
        }

        // 例外時の表示更新
        private void HandleCalculationException(string message)
        {
            SetErrorState(message);
        }

        /// <summary>
        /// Clear Entry (CE) キーが押されたときの処理
        /// </summary>
        private void btnClearEntry_Click(object sender, EventArgs e)
        {
            if (ShouldResetOnError())
                return;

            ClearEntryState();
            
        }

        // CE状態リセット
        private void ClearEntryState()
        {
            isClearEntry = true;
            textResult.Text = ZeroValue;
            text_overwrite = true;
            Num_Dot = false;
        }

        /// <summary>
        /// Clear (C) キーが押されたときの処理
        /// </summary>
        private void btnClear_Click(object sender, EventArgs e)
        {
            ResetAllState();
        }

        // 全体リセット
        private void ResetAllState()
        {
            ResetCalculatorState();
            SetButtonsEnabled(true);
        }

        /// <summary>
        /// Backspaceキーが押されたときの処理
        /// </summary>
        private void btnBack_Click(object sender, EventArgs e)
        {
            if (ShouldResetOnError())
                return;

            if (ShouldClearExpressionOnBack())
                return;

            if (text_overwrite)
                return;

            HandleBackspace();
            UpdateTextResultWithCommas();
           
        }

        // 式欄クリア判定
        private bool ShouldClearExpressionOnBack()
        {
            if (textExpression.Text.Length > 0 && textExpression.Text.EndsWith(EqualSymbol))
            {
                textExpression.Text = "";
                return true;
            }
            return false;
        }

        // バックスペース処理
        private void HandleBackspace()
        {
            string currentText = textResult.Text.Replace(",", "");
            if (currentText.Length > 0)
            {
                string newText = currentText.Substring(0, currentText.Length - 1);

                if (string.IsNullOrEmpty(newText) || newText == "-")
                {
                    textResult.Text = ZeroValue;
                    text_overwrite = true;
                    Num_Dot = false;
                }
                else
                {
                    textResult.Text = newText;
                    Num_Dot = textResult.Text.Contains(".");
                }
            }
            else
            {
                ResetCalculatorState();
            }
        }


        /// <summary>
        /// サインチェンジキーが押されたときの処理
        /// </summary>
        private void btnTogglesign_Click(object sender, EventArgs e)
        {
            if (ShouldResetOnError())
                return;

            if (!string.IsNullOrEmpty(textResult.Text))
            {
                ToggleSign();
                UpdateExpressionForToggleSign();
                AdjustFontSize();
            }
        }

        // サイン反転処理
        private void ToggleSign()
        {
            displayValue = GetCurrentValue();
            displayValue = -displayValue;
            textResult.Text = FormatNumberForDisplay(displayValue);
        }

        // 式欄のサイン反転表示更新
        private void UpdateExpressionForToggleSign()
        {
            string currentExpression = textExpression.Text;
            if (currentExpression.EndsWith("="))
            {
                string valueToNegate;
                if (currentExpression.StartsWith("negate("))
                {
                    valueToNegate = currentExpression.TrimStart("negate(".ToCharArray()).TrimEnd(')', ' ', '=');
                }
                else
                {
                    valueToNegate = FormatNumberForExpression(firstValue);
                }
                string newExpression = string.Format("negate({0}) =", valueToNegate);
                textExpression.Text = newExpression;
            }
        }

        /// <summary>
        /// 最前面表示の切り替え
        /// </summary>
        private void btnTopMost_Click(object sender, EventArgs e)
        {
            this.TopMost = !this.TopMost;
        }

        /// <summary>
        /// テキストボックスにカンマ区切りを適用
        /// </summary>
        private void UpdateTextResultWithCommas()
        {
            if (isErrorState || textResult.Text.Contains("e") || textResult.Text.Contains("E")) return;

            string currentText = textResult.Text.Replace(",", "");
            if (string.IsNullOrEmpty(currentText) || currentText == "-" || (currentText == "0" && !Num_Dot)) return;

            bool isNegative = currentText.StartsWith("-");
            if (isNegative)
            {
                currentText = currentText.Substring(1);
            }
            int dotIndex = currentText.IndexOf('.');
            string integerPart = currentText;
            string decimalPart = "";

            if (dotIndex != -1)
            {
                integerPart = currentText.Substring(0, dotIndex);
                decimalPart = currentText.Substring(dotIndex + 1);
            }

            try
            {
                decimal integerValue;
                if (decimal.TryParse(integerPart, NumberStyles.Number, CultureInfo.InvariantCulture, out integerValue))
                {
                    string formattedInteger = integerValue.ToString("#,##0");
                    string newText = formattedInteger;

                    if (dotIndex != -1)
                    {
                        newText += "." + decimalPart;
                    }

                    if (isNegative)
                    {
                        newText = "-" + newText;
                    }
                    textResult.Text = newText;
                }
            }
            catch (FormatException)
            {
                // Do nothing, let the original text remain
            }
        }

        /// <summary>
        /// 計算式表示用のフォーマットを生成
        /// </summary>
        private string FormatNumberForExpression(decimal value)
        {
            return value.ToString("G29", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 指数表記をフォーマット
        /// </summary>
        private string FormatExponential(decimal value)
        {
            string gFormat = value.ToString("G15", CultureInfo.InvariantCulture);
            string expString = gFormat.Contains("E") ? gFormat : decimal.Parse(gFormat).ToString("E", CultureInfo.InvariantCulture);
            expString = expString.Replace("E+", "e+").Replace("E-", "e-");
            string[] parts = expString.Split('e');
            string formattedMantissa = parts[0];

            formattedMantissa = formattedMantissa.TrimEnd('0');
            if (formattedMantissa.EndsWith("."))
            {
                formattedMantissa = formattedMantissa.TrimEnd('.');
            }

            if (!formattedMantissa.Contains("."))
            {
                formattedMantissa += ".";
            }

            string formattedExponent = Regex.Replace(parts[1], @"^(\+|-)(0)(\d+)", "$1$3");
            return formattedMantissa + "e" + formattedExponent;
        }

        /// <summary>
        /// 結果表示用のフォーマットを生成
        /// </summary>
        private string FormatNumberForDisplay(decimal value)
        {
            decimal absValue = Math.Abs(value);
            string formattedValue;

            bool useExponential = (absValue >= 10000000000000000m || (absValue > 0 && absValue < 0.0000001m));

            if (useExponential)
            {
                formattedValue = FormatExponential(value);
            }
            else
            {
                formattedValue = value.ToString("G29", CultureInfo.InvariantCulture);
                if (value % 1 == 0)
                {
                    formattedValue = value.ToString("G0", CultureInfo.InvariantCulture);
                }
            }

            return formattedValue;
        }

        /// <summary>
        /// 表示桁数を取得
        /// </summary>
        private int GetDisplayDigits(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            string cleanText = text.Replace("-", "").Replace(",", "").Replace(".", "").Replace("e", "").Replace("+", "").Replace("-", "");
            return cleanText.Length;
        }

        /// <summary>
        /// 表示テキストからdecimal値を取得
        /// </summary>
        private decimal GetCurrentValue()
        {
            string textWithoutCommas = textResult.Text.Replace(",", "");
            decimal value;
            if (decimal.TryParse(textWithoutCommas, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }
            return 0;
        }

        /// <summary>
        /// テキストの長さに応じてフォントサイズを調整
        /// </summary>
        private void AdjustFontSize()
        {
            if (isErrorState) return;

            int maxDigits;
            string text = textResult.Text.Replace(",", "");

            if (text.Contains("e") || text.Contains("E"))
            {
                maxDigits = MaxExponentialDigits;
            }
            else
            {
                maxDigits = MaxVisibleDigits;
            }

            int length = text.Length;
            if (text.Contains("-")) length--;
            if (text.Contains(".")) length--;

            float newSize = defaultFontSize;
            if (length > maxDigits)
            {
                newSize = defaultFontSize - (length - maxDigits) * FontSizeDecrement;
                if (newSize < MinFontSize)
                {
                    newSize = MinFontSize;
                }
            }

            // newSizeが0以下の場合は何もしない
            if (newSize <= 0) return;

            if (Math.Abs(textResult.Font.Size - newSize) > 0.1f)
            {
                Font oldFont = textResult.Font;
                textResult.Font = new Font(oldFont.FontFamily, newSize, oldFont.Style);
                oldFont.Dispose();
            }
        }

        /// <summary>
        /// 計算結果の四捨五入
        /// </summary>
        private decimal RoundResult(decimal value)
        {
            
            if (Math.Abs(value) >= 10000000000000000m || (Math.Abs(value) > 0 && Math.Abs(value) < 0.0000001m))
            {
                return value;
            }

            
            if (Math.Abs(value) > 0 && Math.Abs(value) < 1)
            {
             
                return Math.Round(value, 16, MidpointRounding.AwayFromZero);
            }

           
            string integerPartStr = Math.Floor(Math.Abs(value)).ToString();
            int integerLength = integerPartStr.Length;

            int decimalPlacesToRound = 16 - integerLength;
            if (decimalPlacesToRound >= 0)
            {
                return Math.Round(value, decimalPlacesToRound, MidpointRounding.AwayFromZero);
            }

          
            return value;
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e) { }
        private void textResult_TextChanged(object sender, EventArgs e)
        {
            AdjustFontSize();
        }
        private void textExpression_TextChanged(object sender, EventArgs e) { }

        /// <summary>
        /// 計算後の表示を更新
        /// </summary>
        /// <param name="result">計算結果</param>
        private void UpdateDisplayAfterCalculation(decimal result)
        {
            decimal roundedResult = RoundResult(result);
            textResult.Text = FormatNumberForDisplay(roundedResult);
            UpdateTextResultWithCommas();
            text_overwrite = true;
            Num_Dot = textResult.Text.Contains(".");
        }
    }
}