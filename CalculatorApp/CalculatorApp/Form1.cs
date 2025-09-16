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
		private decimal FirstValue = 0;

		/// <summary>
		/// 2番目の値を保持する変数
		/// </summary>
		private decimal SecondValue = 0;

		/// <summary>
		/// テキストボックスの上書きモードを示すフラグ
		/// </summary>
		private bool TextOverwrite = false;

		/// <summary>
		/// 小数点が入力されているかを示すフラグ。
		/// </summary>
		private bool NumDot = false;

		/// <summary>
		/// 加算演算子の記号
		/// </summary>
		private const string ADD_SYMBOL = "+";

		/// <summary>
		/// 減算演算子の記号
		/// </summary>
		private const string SUBTRACT_SYMBOL = "-";

		/// <summary>
		/// 乗算演算子の記号
		/// </summary>
		private const string MULTIPLY_SYMBOL = "×";

		/// <summary>
		/// 除算演算子の記号</summary>
		private const string DIVIDE_SYMBOL = "÷";

		/// <summary>
		/// 等号演算子の記号
		/// </summary>
		private const string EQUAL_SYMBOL = "=";

		/// <summary>
		/// 初期値:0
		/// </summary>
		private const decimal INITIAL_VALUE = 0m;

		/// <summary>
		/// 表示値:0
		/// </summary>
		private const string ZERO_VALUE = "0";

		/// <summary>
		/// ％を小数に変換する乗数
		/// </summary>
		private const decimal PERCENT_MULTIPLY = 0.01m;

		/// <summary>
		/// エラーメッセージフォントサイズ
		/// </summary>
		private const float ERROR_FONT_SIZE = 20.0f;

		/// <summary>
		/// オーバフローが発生したときのエラーメッセージ
		/// </summary>
		private const string ERROR_MESSAGE_OVERFLOW = "計算範囲を超えました";

		/// <summary>
		/// 0除算が発生したときのエラーメッセージ
		/// </summary>
		private const string ERROR_MASSAGE_DIVIDE_BY_ZERO = "0で割ることはできません";

		/// <summary>
		/// 0÷0が行われた時のエラーメッセージ
		/// </summary>
		private const string ERROR_MASSAGE_UNDEFINED = "結果が定義されていません";

		/// <summary>
		/// サインチェンジキーを入力した際に表示される途中結果表示欄のnegate
		/// </summary>
		private const string NEGATE_DISPLAY_FUNCTION = "negate";

		private bool IsNegated = false;

		/// <summary>
		/// 表示桁数
		/// </summary>
		private const int MAX_INTEGER_DISPLAY_DIGITS = 16;

		/// <summary>
		/// 0.から始まる場合の表示桁数
		/// </summary>
		private const int MAX_FRACTION_DISPLAY_DIGITS_LEADING_ZERO = 17;

		/// <summary>
		/// 計算結果表示欄の基準フォントサイズ
		/// </summary>
		private const float RESULT_DISPLAY_BASE_FONT_SIZE = 36f;

		/// <summary>
		/// 途中計算結果欄の基準フォントサイズ
		/// </summary>
		private const float EXPRESSION_DISPLAY_BASE_FONT_SIZE = 10f;

		/// <summary>
		/// フォントの下限サイズ
		/// </summary>
		private const float MIN_FONT_SIZE_LIMIT = 14f;

		/// <summary>
		/// フォントの縮小幅
		/// </summary>
		private const float FONT_SIZE_REDUCTION_STEP = 0.5f;

		/// <summary>
		/// 計算結果表示欄の現在のフォントサイズ
		/// </summary>
		private float defaultFontSize;

		/// <summary>
		/// 途中計算表示欄の現在のフォントサイズ
		/// </summary>
		private float defaultExpressionFontSize;

		/// <summary>
		/// エラー判定フラグ
		/// </summary>
		private bool IsErrorState = false;

		/// <summary>
		/// 現在の表示内容をクリアして新しい値を入力するかどうかを示すフラグ
		/// </summary>
		private bool IsClearEntry = false;

		/// <summary>
		/// 電卓の画面に現在表示されている数値を保持
		/// </summary>
		private decimal DisplayValue = INITIAL_VALUE;

		/// <summary>
		/// サインチェンジキーを押したときの、末端のゼロや小数点の状態を保持するか判定するためのフラグ
		/// </summary>
		private bool PreserveFormatOnToggle = false;

		/// <summary>
		/// 入力した文字列を保持
		/// </summary>
		private string lastUserTypedRaw = ZERO_VALUE;

		/// <summary>
		/// 直前の操作がパーセントキーだったかどうかを示すフラグ
		/// </summary>
		private bool lastActionWasPercent = false;

		/// <summary>
		/// エラー時に操作無効なキー
		/// </summary>
		private Button[] DisabledButtonsOnError;

		/// <summary>
		/// 
		/// </summary>
		private bool ClearedExprAfterEqual = false;

		/// <summary>
		/// 演算子の種類を定義する列挙型
		/// </summary>
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

		/// <summary>現在の演算子種別を保持する変数</summary>
		private OperatorType currentOperatorType = OperatorType.NON;

		/// <summary>
		/// フォームのコンストラクタ
		/// </summary>
		public Form1()
		{
			InitializeComponent();

			// ディスプレイサイズ固定化
			this.MaximumSize = this.Size;
			this.MinimumSize = this.Size;
			this.FormBorderStyle = FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;

			// エラー時に無効化するボタン
			DisabledButtonsOnError = new Button[]
            {
                btnDot, btnTogglesign, btnPercent, btnPlus,
                btnMinus, btnMultiply, btnDivide, btnEnter
            };
		}

		/// <summary>
		/// フォームの初期化処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void Form1_Load(object sender, EventArgs e)
		{
			textResult.Text = ZERO_VALUE;
			TextOverwrite = true;

			textResult.Font = new Font(textResult.Font.FontFamily, RESULT_DISPLAY_BASE_FONT_SIZE, textResult.Font.Style);
			textExpression.Font = new Font(textExpression.Font.FontFamily, EXPRESSION_DISPLAY_BASE_FONT_SIZE, textExpression.Font.Style);

			// 基準サイズの保持
			defaultFontSize = textResult.Font.Size;
			defaultExpressionFontSize = textExpression.Font.Size;

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
		/// 結果表示欄のフォントサイズを初期化
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void textResult_TextChanged(object sender, EventArgs e)
		{
			AutoFitResultFont();
		}

		/// <summary>
		/// 
		/// </summary>
		private void textExpression_TextChanged(object sender, EventArgs e)
		{


		}

		/// <summary>
		/// 数字ボタン押下時のイベントハンドラー
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnNum_Click(object sender, EventArgs e)
		{
			Button btn = sender as Button;
			if (btn == null)
			{
				return;
			}
			if (IsError())
			{
				ResetCalculatorState();
			}
			OnDigitButton(btn.Text);
		}

		/// <summary>
		/// 小数点キー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnDot_Click(object sender, EventArgs e)
		{
			HandleInitialState();
			OnDotButton();
		}

		/// <summary>
		/// 演算子キー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnOperation_Click(object sender, EventArgs e)
		{
			Button btn = (Button)sender;
			OperatorType op = OperatorType.NON;

			switch (btn.Text)
			{
				case ADD_SYMBOL:
					{
						op = OperatorType.ADD;
						break;
					}
				case SUBTRACT_SYMBOL:
					{
						op = OperatorType.SUBTRACT;
						break;
					}
				case MULTIPLY_SYMBOL:
					{
						op = OperatorType.MULTIPLY;
						break;
					}
				case DIVIDE_SYMBOL:
					{
						op = OperatorType.DIVIDE;
						break;
					}
			}
			OnOperatorButton(op);
		}

		/// <summary>
		/// イコールキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnEnter_Click(object sender, EventArgs e)
		{
			OnEqualsButton();
		}

		/// <summary>
		/// ％キー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnPercent_Click(object sender, EventArgs e)
		{
			OnPercentButton();
		}

		/// <summary>
		/// クリアエントリーキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnClearEntry_Click(object sender, EventArgs e)
		{
			OnClearEntryButton();
		}

		/// <summary>
		/// クリアキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnClear_Click(object sender, EventArgs e)
		{
			OnClearButton();
		}

		/// <summary>
		/// 桁下げキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnBack_Click(object sender, EventArgs e)
		{
			OnBackspaceButton();
		}

		/// <summary>
		///サインチェンジキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnTogglesign_Click(object sender, EventArgs e)
		{
			OnToggleSignButton();
		}

		/// <summary>
		/// 最前面表示キー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnTopMost_Click(object sender, EventArgs e)
		{
			this.TopMost = !this.TopMost;
		}

		/// <summary>
		/// 数字キー入力のメイン処理。
		/// ユーザーが押した数字を、現在の電卓の状態に応じて処理
		/// </summary>
		/// <param name="digit">入力された数字を表す文字列</param>
		private void OnDigitButton(string digit)
		{
			HandleInitialState();
			SetButtonsEnabled(true);
			lastActionWasPercent = false;

			if (IsExponentDisplay())
			{
				TextOverwrite = true;
				NumDot = false;
			}

			string current = textResult.Text.Replace(",", "");
			if (!IsInputValid(current, digit))
			{
				return;
			}

			if (TextOverwrite)
			{
				StartNewNumber(digit);
			}
			else
			{
				AppendDigit(digit);
			}

			UpdateTextResultWithCommas();
			IsClearEntry = false;

			PreserveFormatOnToggle = true;
			lastUserTypedRaw = textResult.Text.Replace(",", "");
		}

		/// <summary>
		/// 小数点キー入力のメイン処理。
		/// 小数点の重複入力を防ぎ、入力状態に応じて表示を更新
		/// </summary>
		private void OnDotButton()
		{
			HandleInitialState();
			lastActionWasPercent = false;

			if (NumDot)
			{
				return;
			}

			if (TextOverwrite)
			{
				textResult.Text = "0.";
				TextOverwrite = false;
			}
			else
			{
				textResult.Text += ".";
			}
			NumDot = true;

			PreserveFormatOnToggle = true;
			lastUserTypedRaw = textResult.Text.Replace(",", "");
		}

		/// <summary>
		/// 演算子キー入力のメイン処理。
		/// 現在の状態（直前の操作、入力値、保留中の計算）を考慮し、
		/// 適切な計算を実行して、結果と表示を更新
		/// </summary>
		/// <param name="op">入力された演算子タイプ</param>
		private void OnOperatorButton(OperatorType op)
		{
			if (IsError())
			{
				ResetCalculatorState();
				return;
			}

			try
			{
				if (lastActionWasPercent && currentOperatorType != OperatorType.NON && !ExpressionEndsWithEqual())
				{
					decimal cur = GetCurrentValue();
					PerformPendingCalculation(cur);
					if (IsError())
					{
						return;
					}

					DisplayNumber(FirstValue, true);
					currentOperatorType = op;
					UpdateExpressionDisplay(FirstValue, currentOperatorType);

					lastActionWasPercent = false;
					TextOverwrite = true;
					NumDot = false;
					return;
				}

				if (IsClearEntry)
				{
					currentOperatorType = op;
					UpdateExpressionDisplay(FirstValue, currentOperatorType);
					DisplayNumber(FirstValue, true);
					IsClearEntry = false;
					return;
				}


				if (TextOverwrite && currentOperatorType != OperatorType.NON && !ExpressionEndsWithEqual())
				{
					currentOperatorType = op;
					UpdateExpressionDisplay(FirstValue, currentOperatorType);
				}
				else
				{
					decimal currentValue = GetCurrentValue();
					PerformPendingCalculation(currentValue);
					if (IsError())
					{
						return;
					}

					DisplayNumber(FirstValue, true);
					currentOperatorType = op;
					UpdateExpressionDisplay(FirstValue, currentOperatorType);
				}

				TextOverwrite = true;
				NumDot = false;
				lastActionWasPercent = false;
				PreserveFormatOnToggle = false;
			}
			catch (OverflowException)
			{
				SetErrorState(ERROR_MESSAGE_OVERFLOW);
			}
		}

		/// <summary>
		/// イコールキーのメイン処理
		/// 保留中の計算を最終確定し、結果を表示 
		/// </summary>
		private void OnEqualsButton()
		{
			if (ShouldResetOnError())
			{
				return;
			}

			try
			{
				decimal result = ProcessEqualsLogic();
				if (IsError())
				{
					return;
				}
				DisplayNumber(result, true);

				PreserveFormatOnToggle = false;
				lastActionWasPercent = false;
			}
			catch (InvalidOperationException ex)
			{
				SetErrorState(ex.Message);
			}
			catch (OverflowException)
			{
				SetErrorState(ERROR_MESSAGE_OVERFLOW);
			}
		}

		/// <summary>
		/// '%'キーが押されたときのイベントハンドラ。
		/// 計算機の現在の状態に基づいて、
		/// パーセント計算を行い、画面の表示を更新
		/// </summary>
		private void OnPercentButton()
		{
			if (ShouldResetOnError())
			{
				return;
			}

			if (ExpressionEndsWithEqual())
			{
				try
				{
					decimal r = GetCurrentValue();
					decimal v = r * CalculatePercent(r);

					FirstValue = v;
					SecondValue = INITIAL_VALUE;
					currentOperatorType = OperatorType.NON;

					DisplayNumber(v, true);
					textExpression.Text = FormatNumberForExpression(v);

					lastActionWasPercent = true;
					PreserveFormatOnToggle = false;
					return;
				}
				catch (OverflowException)
				{
					SetErrorState(ERROR_MESSAGE_OVERFLOW);
					return;
				}
			}

			if (currentOperatorType != OperatorType.NON)
			{
				decimal rhs = TextOverwrite ? 0m : GetCurrentValue();
				decimal percentValue = CalculatePercent(rhs); 
				UpdatePercentDisplay(percentValue);           

				lastActionWasPercent = true;
				PreserveFormatOnToggle = false;
				return;
			}

			
			try
			{
				decimal result = 0m;
				DisplayNumber(result, true);
				textExpression.Text = "0";

				// 計算状態をリセット
				FirstValue = result;
				SecondValue = INITIAL_VALUE;
				currentOperatorType = OperatorType.NON;

				lastActionWasPercent = true;
				PreserveFormatOnToggle = false;
				return;
			}
			catch (OverflowException)
			{
				SetErrorState(ERROR_MESSAGE_OVERFLOW);
				return;
			}
		}


		/// <summary>
		/// クリアエントリーキー入力の処理。式は維持し、表示を 0 に戻す。
		/// </summary>
		private void OnClearEntryButton()
		{
			// エラーの場合はリセット
			if (ShouldResetOnError())
			{
				return;
			}

			string currentExpression = textExpression.Text != null ? textExpression.Text.Trim() : string.Empty;

			if (ExpressionEndsWithEqual())
			{
				if (HasBinaryOperatorInExpression(currentExpression))
				{
					ResetCalculatorState();  
					SetButtonsEnabled(true);  
					return;  
				}

				DisplayZeroResult();  
				ResetCalculationValues(); 
				return;  
			}

			ClearCurrentEntry();
		}

		/// <summary>
		/// 数式列に演算子が含まれているかを判定
		/// ' = '記号より前の部分のみを検査し、加算、減算、乗算、除算のいずれかが含まれていればtrueを返す
		/// </summary>
		/// <param name="expr">検査対象となる数式</param>
		/// <returns>演算子が含まれていればtrue、そうでなければfalse</returns>
		private bool HasBinaryOperatorInExpression(string expr)
		{
			if (string.IsNullOrEmpty(expr))
			{
				return false;
			}

			int eq = expr.LastIndexOf(EQUAL_SYMBOL);

			string body;
			if (eq >= 0)
			{
				body = expr.Substring(0, eq);
			}
			else
			{
				body = expr;
			}

			if (body.IndexOf(" " + ADD_SYMBOL + " ", StringComparison.Ordinal) >= 0)
			{
				return true;
			}

			if (body.IndexOf(" " + SUBTRACT_SYMBOL + " ", StringComparison.Ordinal) >= 0)
			{
				return true;
			}

			if (body.IndexOf(" " + MULTIPLY_SYMBOL + " ", StringComparison.Ordinal) >= 0)
			{
				return true;
			}

			if (body.IndexOf(" " + DIVIDE_SYMBOL + " ", StringComparison.Ordinal) >= 0)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// 電卓の表示を'0'に設定
		/// </summary>
		private void DisplayZeroResult()
		{
			textResult.Text = ZERO_VALUE;
			TextOverwrite = true;
			NumDot = false;

			PreserveFormatOnToggle = false;
			lastUserTypedRaw = ZERO_VALUE;
			lastActionWasPercent = false;
		}

		/// <summary>
		/// 計算状態を初期値に戻す
		/// </summary>
		private void ResetCalculationValues()
		{
			FirstValue = INITIAL_VALUE;
			SecondValue = INITIAL_VALUE;
			currentOperatorType = OperatorType.NON;
		}

		/// <summary>
		/// 現在の数値をクリアし、表示を０に戻す
		/// </summary>
		private void ClearCurrentEntry()
		{
			IsClearEntry = true;
			DisplayZeroResult();  
		}

		/// <summary>
		/// クリアキーの処理。全状態を初期化する。
		/// </summary>
		private void OnClearButton()
		{
			ResetAllState();
		}

		/// <summary>
		/// 桁下げキー入力の処理
		/// 末尾1文字削除を行う。
		/// </summary>
		private void OnBackspaceButton()
		{
			if (ShouldResetOnError())
			{
				return;
			}

			if (ExpressionEndsWithEqual())
			{
				textExpression.Text = "";
				TextOverwrite = true;
				NumDot = false;
				PreserveFormatOnToggle = false;
				lastActionWasPercent = false;
				ClearedExprAfterEqual = true;
				return;
			}

			if (ClearedExprAfterEqual)
			{
				return;
			}

			if (IsExponentDisplay())
			{
				TextOverwrite = true;
				NumDot = false;
				textResult.Text = ZERO_VALUE;

				PreserveFormatOnToggle = false;
				lastUserTypedRaw = ZERO_VALUE;
				lastActionWasPercent = false;
				return;
			}

			if (TextOverwrite)
			{
				return;
			}

			HandleBackspace();
			UpdateTextResultWithCommas();


			PreserveFormatOnToggle = true;
			lastUserTypedRaw = textResult.Text.Replace(",", "");
			lastActionWasPercent = false;
		}

		/// <summary>
		/// サインチェンジキーの処理。ユーザー入力の見た目保持（末尾ゼロ維持）と、
		/// ＝直後の negate(...) 表記更新に対応する。
		/// </summary>
		private void OnToggleSignButton()
		{
			if (ShouldResetOnError())
			{
				return;
			}

			if (string.IsNullOrEmpty(textResult.Text))
			{
				return;
			}

			if (PreserveFormatOnToggle && !IsExponentDisplay())
			{
				string raw = textResult.Text.Replace(",", "");
				raw = ToggleSignRaw(raw);
				SetTextFromRawPreservingCommas(raw);


				TextOverwrite = false;
				NumDot = (raw.IndexOf('.') >= 0);


				UpdateExpressionForToggleSign();


				lastUserTypedRaw = raw;
				lastActionWasPercent = false;
				return;
			}

			ToggleSign();
			UpdateExpressionForToggleSign();

			PreserveFormatOnToggle = false;
			lastActionWasPercent = false;
		}


		/// <summary>
		/// 新しい数値入力を開始する。上書きモードを解除し、小数点フラグを更新。
		/// </summary>
		/// <param name="digit">新しい数値の最初の桁を表す文字列。</param>
		private void StartNewNumber(string digit)
		{
			textResult.Text = digit;
			TextOverwrite = false;
			NumDot = (digit == ".");
		}

		/// <summary>
		/// 既存の入力の末尾に数字を追加する。
		/// </summary>
		/// <param name="digit">追加する数字を表す文字列。</param>
		private void AppendDigit(string digit)
		{
			textResult.Text += digit;
		}

		/// <summary>
		/// ユーザーの入力が有効であるか検証
		/// 最大表示桁数の超過、重複したゼロ、不適切な小数点の入力などをチェック
		/// </summary>
		/// <param name="currentText">現在の表示。</param>
		/// <param name="digit">新たに入力された数字または記号。</param>
		/// <returns>入力が有効であればtrue、そうでなければfalse。</returns>
		private bool IsInputValid(string currentText, string digit)
		{
			bool startsWithZeroDot = currentText.StartsWith("0.") || currentText.StartsWith("-0.");
			int maxDigits = startsWithZeroDot ? MAX_FRACTION_DISPLAY_DIGITS_LEADING_ZERO : MAX_INTEGER_DISPLAY_DIGITS;

			string nextText = TextOverwrite ? digit : currentText + digit;
			int nextLength = nextText.Replace(".", "").Replace("-", "").Length;

			if (nextLength > maxDigits)
			{
				return false;
			}

			if (!TextOverwrite && currentText == ZERO_VALUE && digit == ZERO_VALUE && !NumDot)
			{
				return false;
			}

			return true;
		}


		/// <summary>
		/// 指定された演算子に基づき、演算子の記号を返す。
		/// </summary>
		/// <param name="type">演算子</param>
		/// <returns>指定された演算子に対応する記号</returns>
		private string GetOperatorSymbol(OperatorType type)
		{
			switch (type)
			{
				case OperatorType.ADD:
					{
						return ADD_SYMBOL;
					}
				case OperatorType.SUBTRACT:
					{
						return SUBTRACT_SYMBOL;
					}
				case OperatorType.MULTIPLY:
					{
						return MULTIPLY_SYMBOL;
					}
				case OperatorType.DIVIDE:
					{
						return DIVIDE_SYMBOL;
					}
				default:
					{
						return string.Empty;
					}
			}
		}

		/// <summary>
		/// 左辺と右辺の値を指定された演算子タイプに基づいて計算する
		/// </summary>
		/// <param name="left">左辺の値</param>
		/// <param name="right">右辺の値</param>
		/// <param name="type">演算子のタイプ</param>
		/// <returns>計算結果</returns>
		private decimal Calculate(decimal left, decimal right, OperatorType type)
		{
			switch (type)
			{
				case OperatorType.ADD:
					{
						return left + right;
					}
				case OperatorType.SUBTRACT:
					{
						return left - right;
					}
				case OperatorType.MULTIPLY:
					{
						return left * right;
					}
				case OperatorType.DIVIDE:
					{
						return left / right;
					}
				default:
					{
						return right;
					}
			}
		}

		/// <summary>
		/// 保留中の演算を解決する。未選択なら左辺を現在値に更新し、0除算や0÷0 はエラー化する。
		/// </summary>
		private void PerformPendingCalculation(decimal currentValue)
		{
			if (ExpressionEndsWithEqual() || currentOperatorType == OperatorType.NON)
			{
				FirstValue = currentValue;
			}
			else
			{
				if (currentOperatorType == OperatorType.DIVIDE && currentValue == INITIAL_VALUE)
				{
					if (FirstValue == INITIAL_VALUE)
					{
						SetErrorState(ERROR_MASSAGE_UNDEFINED);
					}
					else
					{
						SetErrorState(ERROR_MASSAGE_DIVIDE_BY_ZERO);
					}
					return;
				}

				decimal result = Calculate(FirstValue, currentValue, currentOperatorType);
				FirstValue = result;
			}
		}

		/// <summary>
		/// 途中式表示を更新する
		/// </summary>
		private void UpdateExpressionDisplay(decimal value, OperatorType type)
		{
			string op = GetOperatorSymbol(type);
			string currentExpr = (textExpression.Text != null ? textExpression.Text.Trim() : string.Empty);

			if (!string.IsNullOrEmpty(currentExpr) &&
				!currentExpr.EndsWith(EQUAL_SYMBOL) &&
				currentExpr.StartsWith(NEGATE_DISPLAY_FUNCTION + "(", StringComparison.Ordinal))
			{
				textExpression.Text = currentExpr + " " + op;
				return;
			}

			textExpression.Text = string.Format("{0} {1}", FormatNumberForExpression(value), op);
		}

		/// <summary>
		///イコールキーの処理
		/// </summary>
		private decimal ProcessEqualsLogic()
		{
			decimal currentValue = GetCurrentValue();
			bool isFirstEqual = !ExpressionEndsWithEqual();

			if (currentOperatorType == OperatorType.NON)
			{
				SecondValue = currentValue;
				textExpression.Text = string.Format("{0} {1}",
					FormatNumberForExpression(currentValue), EQUAL_SYMBOL);
				FirstValue = currentValue;
				return currentValue;
			}

			decimal left, right;

			if (isFirstEqual)
			{
				left = FirstValue;
				right = currentValue;
				SecondValue = currentValue;
			}
			else
			{
				left = FirstValue;
				right = SecondValue;
			}

			if (currentOperatorType == OperatorType.DIVIDE && right == INITIAL_VALUE)
			{
				if (left == INITIAL_VALUE)
				{
					throw new InvalidOperationException(ERROR_MASSAGE_UNDEFINED);
				}
				else
				{
					throw new InvalidOperationException(ERROR_MASSAGE_DIVIDE_BY_ZERO);
				}
			}

			decimal result = Calculate(left, right, currentOperatorType);
			FirstValue = result;

			string opSym = GetOperatorSymbol(currentOperatorType);
			string leftExpr = FormatNumberForExpression(left);
			string rightExpr = FormatNumberForExpression(right);

			string curr = (textExpression.Text != null ? textExpression.Text.Trim() : string.Empty);

			if (!string.IsNullOrEmpty(curr) &&
				!curr.EndsWith(EQUAL_SYMBOL) &&
				curr.StartsWith(NEGATE_DISPLAY_FUNCTION + "(", StringComparison.Ordinal))
			{
				if (curr.EndsWith(opSym))
				{
					textExpression.Text = curr + " " + rightExpr + " " + EQUAL_SYMBOL;
				}
				else
				{
					textExpression.Text = curr + " " + EQUAL_SYMBOL;
				}
			}
			else
			{
				textExpression.Text = string.Format("{0} {1} {2} {3}",
					leftExpr, opSym, rightExpr, EQUAL_SYMBOL);
			}

			return result;
		}


		/// <summary>
		/// 現在値を％形式に変換する。
		/// </summary>
		private decimal CalculatePercent(decimal value)
		{
			return value * PERCENT_MULTIPLY;
		}

		/// <summary>
		/// ％の表示と式を更新する。加減算は A*(B/100)、乗除算は B/100 として扱う。
		/// 表示は編集継続可能な状態にする。
		/// </summary>
		private void UpdatePercentDisplay(decimal percentValue)
		{
			decimal previousValue = FirstValue;
			decimal calculatedValue;

			if (currentOperatorType == OperatorType.ADD || currentOperatorType == OperatorType.SUBTRACT)
			{
				calculatedValue = previousValue * percentValue;
				textExpression.Text = string.Format("{0} {1} {2}",
					FormatNumberForExpression(previousValue),
					GetOperatorSymbol(currentOperatorType),
					FormatNumberForExpression(calculatedValue));
			}
			else
			{
				calculatedValue = percentValue; // B% = B/100
				textExpression.Text = string.Format("{0} {1} {2}",
					FormatNumberForExpression(previousValue),
					GetOperatorSymbol(currentOperatorType),
					FormatNumberForExpression(calculatedValue));
			}


			DisplayNumber(calculatedValue, false);
		}

		/// <summary>
		/// 結果表示の末尾 1 文字を削除する。空や単独の「-」になった場合は 0 に戻す。
		/// </summary>
		private void HandleBackspace()
		{
			string currentText = textResult.Text.Replace(",", "");
			if (currentText.Length > 0)
			{
				string newText = currentText.Substring(0, currentText.Length - 1);

				if (string.IsNullOrEmpty(newText) || newText == "-")
				{
					textResult.Text = ZERO_VALUE;
					TextOverwrite = true;
					NumDot = false;
				}
				else
				{
					textResult.Text = newText;
					NumDot = textResult.Text.Contains(".");
				}
			}
			else
			{
				ResetCalculatorState();
			}
		}

		/// <summary>
		/// 数値として符号反転を行い表示を更新
		/// </summary>
		private void ToggleSign()
		{
			DisplayValue = GetCurrentValue();
			DisplayValue = -DisplayValue;
			DisplayNumber(DisplayValue, false);
			IsNegated = true;
		}

		/// <summary>
		/// カンマ無しの文字列の符号のみを反転する
		/// </summary>
		private string ToggleSignRaw(string raw)
		{
			if (string.IsNullOrEmpty(raw))
			{
				return raw;
			}
			if (raw[0] == '-')
			{
				return raw.Substring(1);
			}
			return "-" + raw;
		}

		/// <summary>
		/// 文字列をそのまま結果欄に反映し、カンマ付与を行う。
		/// </summary>
		private void SetTextFromRawPreservingCommas(string raw)
		{
			textResult.Text = raw;
			UpdateTextResultWithCommas();
		}

		/// <summary>
		/// イコールキー入力直後の サインチェンジキーを入力したとき negate(...) の入れ子表記で更新する。
		/// 例）"100 =" → "negate(100) " → さらに ± → "negate(negate(100))"
		/// </summary>
		private void UpdateExpressionForToggleSign()
		{
			string expr = (textExpression.Text != null ? textExpression.Text.Trim() : string.Empty);

			if (expr.EndsWith(EQUAL_SYMBOL))
			{
				int eq = expr.LastIndexOf(EQUAL_SYMBOL);
				string body = (eq >= 0 ? expr.Substring(0, eq) : expr).Trim();
				if (string.IsNullOrEmpty(body)) body = FormatNumberForExpression(FirstValue);

				textExpression.Text = NEGATE_DISPLAY_FUNCTION + "(" + body + ")";
				return;
			}

			if (expr.StartsWith(NEGATE_DISPLAY_FUNCTION + "(", StringComparison.Ordinal))
			{
				textExpression.Text = NEGATE_DISPLAY_FUNCTION + "(" + expr + ")";
			}
		}


		/// <summary>
		/// 表示の丸め、 カンマ付与 、 状態更新を行う。
		/// </summary>
		private void DisplayNumber(decimal value, bool overwrite)
		{
			decimal rounded = RoundResult(value);
			textResult.Text = FormatNumberForDisplay(rounded);
			UpdateTextResultWithCommas();
			TextOverwrite = overwrite;
			NumDot = false;

			PreserveFormatOnToggle = false;
			lastUserTypedRaw = textResult.Text.Replace(",", "");
		}

		/// <summary>
		/// 結果表示の文字列に 3 桁区切りカンマを付与する
		/// </summary>
		private void UpdateTextResultWithCommas()
		{
			if (IsError())
			{
				return;
			}
			if (IsExponentDisplay())
			{
				return;
			}

			string currentText = textResult.Text.Replace(",", "");
			if (string.IsNullOrEmpty(currentText) || currentText == "-" || (currentText == "0" && !NumDot))
			{
				return;
			}

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
					string formattedInteger = integerValue.ToString("#,##0", CultureInfo.InvariantCulture);
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
				// 何もしない
			}
		}

		/// <summary>
		/// 途中式用の数値整形を行う。
		/// </summary>
		private string FormatNumberForExpression(decimal value)
		{
			return FormatNumberForDisplay(value);
		}

		/// <summary>
		/// 指数表記の整形を行う。指数は小文字 e、不要な 0/小数点を整理する。
		/// </summary>
		private string FormatExponential(decimal value)
		{
			string gFormat = value.ToString("G15", CultureInfo.InvariantCulture);
			string expString = gFormat.Contains("E")
				? gFormat
				: decimal.Parse(gFormat, CultureInfo.InvariantCulture).ToString("E", CultureInfo.InvariantCulture);

			expString = expString.Replace("E+", "e+").Replace("E-", "e-");
			string[] parts = expString.Split(new char[] { 'e' });

			string mantissa = parts[0].TrimEnd('0');
			if (mantissa.EndsWith("."))
			{
				mantissa = mantissa.TrimEnd('.');
			}
			if (!mantissa.Contains("."))
			{
				mantissa += ".";
			}

			string exponent = Regex.Replace(parts[1], @"^(\+|-)(0)(\d+)", "$1$3");
			return mantissa + "e" + exponent;
		}

		/// <summary>
		/// 数値の大きさが表示可能な桁数を超える場合、指数表記に変換する
		/// </summary>
		private string FormatNumberForDisplay(decimal value)
		{
			decimal abs = Math.Abs(value);
			if (abs == 0m)
			{
				return ZERO_VALUE;
			}


			string fixedStr = value.ToString("0.#############################", CultureInfo.InvariantCulture);

			if (abs >= 1m)
			{

				int dot = fixedStr.IndexOf('.');
				bool neg = (fixedStr[0] == '-');
				int intLen = (dot >= 0 ? dot : fixedStr.Length) - (neg ? 1 : 0);

				if (intLen > MAX_INTEGER_DISPLAY_DIGITS)
				{
					return FormatExponential(value);
				}
				return fixedStr;
			}
			else
			{

				int dot = fixedStr.IndexOf('.');
				int fracLen = (dot >= 0) ? (fixedStr.Length - dot - 1) : 0;

				if (fracLen > MAX_FRACTION_DISPLAY_DIGITS_LEADING_ZERO)
				{
					return FormatExponential(value);
				}
				return fixedStr;
			}
		}

		/// <summary>
		/// 現在の表示文字列を decimal に変換する
		/// </summary>
		private decimal GetCurrentValue()
		{
			return ParseDisplayToDecimal(textResult.Text);
		}

		/// <summary>
		/// 表示文字列を decimal に変換する
		/// </summary>
		private decimal ParseDisplayToDecimal(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return INITIAL_VALUE;
			}

			string s = text.Replace(",", "");
			decimal dv;

			if (s.IndexOf('e') >= 0 || s.IndexOf('E') >= 0)
			{
				double dd;
				if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out dd))
				{
					try
					{
						return (decimal)dd;
					}
					catch
					{
						return INITIAL_VALUE;
					}
				}
				return INITIAL_VALUE;
			}

			if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out dv))
			{
				return dv;
			}
			return INITIAL_VALUE;
		}

		/// <summary>
		/// 数値の絶対値に応じて計算結果を丸める
		/// </summary>
		private decimal RoundResult(decimal value)
		{
			decimal abs = Math.Abs(value);


			if (abs > 0m && abs < 1m)
			{
				return Math.Round(value, 16, MidpointRounding.AwayFromZero);
			}

			if (abs >= 1m)
			{
				string integerPartStr = Math.Floor(abs).ToString(CultureInfo.InvariantCulture);
				int integerLength = integerPartStr.Length;

				int decimalPlacesToRound = 16 - integerLength;
				if (decimalPlacesToRound >= 0)
				{
					return Math.Round(value, decimalPlacesToRound, MidpointRounding.AwayFromZero);
				}
			}

			return value;
		}


		/// <summary>
		/// エラー状態かどうかを返す。
		/// </summary>
		private bool IsError()
		{
			return IsErrorState;
		}

		/// <summary>
		/// 式表示が ＝ で終わっているか判定する
		/// </summary>
		private bool ExpressionEndsWithEqual()
		{
			return textExpression.Text.Length > 0 && textExpression.Text.EndsWith(EQUAL_SYMBOL);
		}

		/// <summary>
		/// 現在の結果表示が指数表記か判定する。
		/// </summary>
		private bool IsExponentDisplay()
		{
			string t = textResult.Text;
			return (t.IndexOf('e') >= 0 || t.IndexOf('E') >= 0);
		}

		/// <summary>
		/// 
		/// </summary>
		private bool ShouldResetOnError()
		{
			if (IsErrorState)
			{
				ResetCalculatorState();
				return true;
			}
			return false;
		}

		/// <summary>
		/// 結果表示欄のフォントサイズを初期化
		/// </summary>
		private void AutoFitResultFont()
		{
			float size = defaultFontSize; 

			FontFamily family = textResult.Font.FontFamily;
			FontStyle style = textResult.Font.Style;

			while (size > MIN_FONT_SIZE_LIMIT)
			{
				using (Font trial = new Font(family, size, style))
				{
					Size proposed = new Size(int.MaxValue, int.MaxValue);
					TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
					Size sz = TextRenderer.MeasureText(textResult.Text, trial, proposed, flags);

					if (sz.Width <= textResult.ClientSize.Width)
					{
						if (Math.Abs(textResult.Font.Size - size) > 0.1f)
						{
							Font old = textResult.Font;
							textResult.Font = new Font(family, size, style);
							old.Dispose();
						}
						return;
					}
				}
				size -= FONT_SIZE_REDUCTION_STEP;
			}

			if (Math.Abs(textResult.Font.Size - MIN_FONT_SIZE_LIMIT) > 0.1f)
			{
				Font oldFinal = textResult.Font;
				textResult.Font = new Font(family, MIN_FONT_SIZE_LIMIT, style);
				oldFinal.Dispose();
			}
		}

		/// <summary>
		/// エラー時に一部キーの活性/非活性をまとめて切り替える。
		/// </summary>
		private void SetButtonsEnabled(bool enabled)
		{
			foreach (Button btn in DisabledButtonsOnError)
			{
				btn.Enabled = enabled;
			}
		}

		/// <summary>
		/// エラー状態を設定し、メッセージ表示・フォントサイズ調整・ボタン無効化を行う。
		/// </summary>
		private void SetErrorState(string message)
		{
			textResult.Text = message;

			float sz = ERROR_FONT_SIZE;
			if (sz < MIN_FONT_SIZE_LIMIT)
			{
				sz = MIN_FONT_SIZE_LIMIT;
			}
			textResult.Font = new Font(textResult.Font.FontFamily, sz, textResult.Font.Style);

			IsErrorState = true;
			SetButtonsEnabled(false);
		}

		/// <summary>
		/// 入力開始前の状態を整える。エラー中や ＝直後の場合は状態を初期化する。
		/// </summary>
		private void HandleInitialState()
		{
			if (IsErrorState || ExpressionEndsWithEqual())
			{
				ResetCalculatorState();
				ClearedExprAfterEqual = false;
			}
		}

		/// <summary>
		/// 全体のリセット処理である。計算状態と UI 状態を初期化し、ボタンを活性化する。
		/// </summary>
		private void ResetAllState()
		{
			ResetCalculatorState();
			SetButtonsEnabled(true);
		}

		/// <summary>
		/// 計算機内部状態と表示、フォントの初期化を行う
		/// </summary>
		private void ResetCalculatorState()
		{
			FirstValue = INITIAL_VALUE;
			SecondValue = INITIAL_VALUE;
			currentOperatorType = OperatorType.NON;
			textExpression.Text = "";
			textResult.Text = ZERO_VALUE;
			TextOverwrite = true;
			NumDot = false;
			IsErrorState = false;
			IsClearEntry = false;
			DisplayValue = INITIAL_VALUE;

			PreserveFormatOnToggle = false;
			lastUserTypedRaw = ZERO_VALUE;
			lastActionWasPercent = false;
			ClearedExprAfterEqual = false;


			textResult.Font = new Font(textResult.Font.FontFamily, defaultFontSize, textResult.Font.Style);


			textExpression.Font = new Font(textExpression.Font.FontFamily, defaultExpressionFontSize, textExpression.Font.Style);

			AutoFitResultFont();
		}
	}
}
